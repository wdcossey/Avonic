#!/usr/bin/env node
// Compares two versions of Ionic as Avonic reads them: each component's implementation, its md skin, the
// global CSS and the API in core.json, with what changes on every build taken out first.
//
//   node tools/ionic-diff.js 8.8.1 8.8.19                  # a summary: changed lines per component, and the API
//   node tools/ionic-diff.js 8.8.1 8.8.19 --show checkbox  # the diff of one component's .js and .md.css
//   node tools/ionic-diff.js 8.8.1 8.8.19 --show css       # the diff of the global CSS (--show utils: of utils/)
//   node tools/ionic-diff.js 8.8.1 8.8.19 --checklists     # the diff of every tag's ionic-api.js checklist
//   node tools/ionic-diff.js 8.8.1 8.8.19 --all            # the summary for every component, not only Avonic's
//
// Normalising:
//   - Stencil's vdom `key: '<hash>'` props, which change on every build, are removed;
//   - `test/` folders, `*.ios.css`, `*.ionic.css` and source maps are left out, as Avonic recreates md only;
//   - trailing white space is dropped;
//   - in `.js`, TypeScript's ES2017 down-levelling of `?.` and `??` (Ionic 8's build) is written back as the operators,
//     as Ionic 9's ES2020 build emits them, and the `var _a;` temporaries it needed are removed. Object spread
//     (`Object.assign` in 8) and other syntax still show, so read a small diff before calling it a change.
//
// The packages come from the registry with `npm pack` and are kept in the system's temp folder, so that a
// second run is offline. Needs `npm`, `tar` and `git` on the PATH (`git diff --no-index` does the diffing).

const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync, spawnSync } = require('child_process');

const args = process.argv.slice(2);
const versions = args.filter(a => /^\d+\.\d+\.\d+/.test(a));
const flag = name => args.includes(name);
const option = name => { const i = args.indexOf(name); return i < 0 ? null : args[i + 1]; };
if (versions.length !== 2) {
    console.error('usage: node tools/ionic-diff.js <old> <new> [--show <component>|css|utils] [--checklists] [--all]');
    process.exit(1);
}

const repo = path.join(__dirname, '..');
const cache = path.join(os.tmpdir(), 'avonic-ionic-diff');
const npm = process.platform === 'win32' ? 'npm.cmd' : 'npm';

/** Downloads and unpacks one package at one version, once. Returns its folder. */
function fetchPackage(name, version) {
    const dir = path.join(cache, `${name.replace(/[@/]/g, '_')}-${version}`);
    const done = path.join(dir, '.done');
    if (fs.existsSync(done)) return path.join(dir, 'package');
    fs.rmSync(dir, { recursive: true, force: true });
    fs.mkdirSync(dir, { recursive: true });
    execFileSync(npm, ['pack', `${name}@${version}`, '--silent', '--pack-destination', dir],
        { stdio: ['ignore', 'ignore', 'inherit'], shell: process.platform === 'win32' });
    const tarball = fs.readdirSync(dir).find(f => f.endsWith('.tgz'));
    // A relative name, from the folder: GNU tar reads `C:` in a path as a remote host.
    execFileSync('tar', ['-xzf', tarball], { cwd: dir, stdio: ['ignore', 'ignore', 'inherit'] });
    fs.writeFileSync(done, '');
    return path.join(dir, 'package');
}

/** Bumped when the normalising changes, as the trees are cached. */
const TREE_FORMAT = 3;

const normalise = (text, file = '') => {
    const out = text
        .replace(/\r\n/g, '\n')
        .replace(/\bkey: '[0-9a-f]{40}',? ?/g, '')
        .replace(/[ \t]+$/gm, '');
    return file.endsWith('.js') ? modernise(out) : out;
};

/**
 * Writes TypeScript's ES2017 down-levelling of `?.` and `??` back as the operators, as Ionic 9's build (Stencil 4.44,
 * ES2020) emits them, so that a diff across that change shows what changed and not how it was compiled. Only the forms
 * TypeScript emits are matched; what's left (`Object.assign` for a spread) still shows.
 */
function modernise(js) {
    // `(_a = EXPR) === null || _a === void 0 ? void 0 : _a.x` and `(_a = EXPR) !== null && _a !== void 0 ? _a : y`.
    let out = '';
    for (let i = 0; i < js.length;) {
        const m = /^\((_[a-z]\d*) = /.exec(js.slice(i, i + 12));
        if (m) {
            let depth = 0, j = i;
            for (; j < js.length; j++) {
                if (js[j] === '(') depth++;
                else if (js[j] === ')' && --depth === 0) break;
            }
            const expr = js.slice(i + m[0].length, j);
            const rest = js.slice(j + 1, j + 1 + 80);
            const v = m[1];
            const chain = `) === null || ${v} === void 0 ? void 0 : ${v}`;
            const coalesce = `) !== null && ${v} !== void 0 ? ${v} : `;
            if ((')' + rest).startsWith(chain)) {
                const after = j + chain.length;
                // `?.x`, `?.(` or `?.[`: the dot is TypeScript's own for a member, and added for a call or an index.
                out += expr + '?.';
                i = js[after] === '.' ? after + 1 : after;
                continue;
            }
            if ((')' + rest).startsWith(coalesce)) {
                out += expr + ' ?? ';
                i = j + coalesce.length;
                continue;
            }
        }
        out += js[i++];
    }
    const ref = String.raw`(?:this|[\w$]+)(?:\.[\w$]+)*`;
    return out
        // `a.b === null || a.b === void 0 ? void 0 : a.b.c` → `a.b?.c`
        .replace(new RegExp(String.raw`(?<![\w$.])(${ref}) === null \|\| \1 === void 0 \? void 0 : \1(\.|(?=[\[(]))`, 'g'),
            (_, r) => `${r}?.`)
        // `a !== null && a !== void 0 ? a : b` → `a ?? b`
        .replace(new RegExp(String.raw`(?<![\w$.])(${ref}) !== null && \1 !== void 0 \? \1 : `, 'g'), '$1 ?? ')
        // `(a?.b) !== c`: the parentheses the conditional needed before a comparison (not an `if (a?.b)`'s)
        .replace(/(?<![\w$)\]])\(([\w$]+(?:\??\.[\w$]+)*\?\.[\w$]+(?:\??\.[\w$]+)*)\)(?= [!=]==)/g, '$1')
        // `var _a, _b;`, the temporaries, and the lint comment that asked for `?.`
        .replace(/^[ \t]*var _[a-z]\d*(?:, _[a-z]\d*)*;\n/gm, '')
        .replace(/^[ \t]*\/\/ eslint-disable-next-line @typescript-eslint\/prefer-optional-chain\n/gm, '');
}

const wanted = file =>
    !/(^|[\\/])test[\\/]/.test(file) &&
    !/\.(ios|ionic)\.css$/.test(file) &&
    !/\.map$/.test(file) &&
    /\.(js|css)$/.test(file);

function copyNormalised(from, to) {
    if (!fs.existsSync(from)) return;
    for (const entry of fs.readdirSync(from, { withFileTypes: true, recursive: true })) {
        if (!entry.isFile()) continue;
        const source = path.join(entry.parentPath ?? entry.path, entry.name);
        const relative = path.relative(from, source);
        if (!wanted(relative)) continue;
        const target = path.join(to, relative);
        fs.mkdirSync(path.dirname(target), { recursive: true });
        fs.writeFileSync(target, normalise(fs.readFileSync(source, 'utf8'), relative));
    }
}

/** The normalised tree of one version: components/, utils/, css/, and core.json. */
function prepare(version) {
    // Named for the normalising too, so that a change to it doesn't read a tree normalised the old way.
    const out = path.join(cache, `tree-${TREE_FORMAT}-${version}`);
    if (fs.existsSync(path.join(out, '.done'))) return out;
    fs.rmSync(out, { recursive: true, force: true });
    const core = fetchPackage('@ionic/core', version);
    const docs = fetchPackage('@ionic/docs', version);
    copyNormalised(path.join(core, 'dist', 'collection', 'components'), path.join(out, 'components'));
    copyNormalised(path.join(core, 'dist', 'collection', 'utils'), path.join(out, 'utils'));
    for (const file of fs.readdirSync(path.join(core, 'css'), { recursive: true })) {
        // The readable global files; the bundles and the minified ones repeat them.
        if (!/\.css$/.test(file) || /\.(min|bundle)\.css$|\.ios\.|\.class\.|\.system\./.test(file)) continue;
        const target = path.join(out, 'css', file);
        fs.mkdirSync(path.dirname(target), { recursive: true });
        fs.writeFileSync(target, normalise(fs.readFileSync(path.join(core, 'css', file), 'utf8')));
    }
    fs.copyFileSync(path.join(docs, 'core.json'), path.join(out, 'core.json'));
    fs.writeFileSync(path.join(out, '.done'), '');
    return out;
}

/** `git diff --no-index`, which exits 1 when the two differ. */
function gitDiff(a, b, extra = []) {
    const r = spawnSync('git', ['diff', '--no-index', '--no-color', ...extra, '--', a, b],
        { encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 });
    if (r.status > 1) throw new Error(r.stderr);
    return r.stdout;
}

/** Changed lines (added + removed) between two files, either of which may be missing. */
function changedLines(a, b) {
    const empty = path.join(cache, 'empty');
    if (!fs.existsSync(empty)) fs.writeFileSync(empty, '');
    const left = fs.existsSync(a) ? a : empty;
    const right = fs.existsSync(b) ? b : empty;
    if (left === empty && right === empty) return null;
    const out = gitDiff(left, right, ['--numstat']);
    if (!out.trim()) return 0;
    const [added, removed] = out.trim().split(/\s+/);
    return Number(added) + Number(removed);
}

const [oldVersion, newVersion] = versions;
const oldTree = prepare(oldVersion);
const newTree = prepare(newVersion);

// What Avonic has: the ion-* folders of docs/components, as component folder names.
const avonicTags = fs.readdirSync(path.join(repo, 'docs', 'components'))
    .filter(d => d.startsWith('ion-'));
const coreOf = tree => JSON.parse(fs.readFileSync(path.join(tree, 'core.json'), 'utf8'));
const oldCore = coreOf(oldTree);
const newCore = coreOf(newTree);

/** ion-item-divider → item-divider/item-divider: where the tag's files are in dist/collection/components. */
function folderOf(tag, tree) {
    const name = tag.replace(/^ion-/, '');
    const direct = path.join(tree, 'components', name, `${name}.js`);
    if (fs.existsSync(direct)) return path.join('components', name, name);
    // A tag in its family's folder, as ion-card-header isn't: look for <name>.js anywhere.
    const hit = fs.readdirSync(path.join(tree, 'components'), { recursive: true })
        .find(f => path.basename(f) === `${name}.js`);
    return hit ? path.join('components', hit.replace(/\.js$/, '')) : null;
}

const show = option('--show');
if (show === 'css' || show === 'utils') {
    process.stdout.write(gitDiff(path.join(oldTree, show), path.join(newTree, show)));
    process.exit(0);
}
if (show) {
    const tag = show.startsWith('ion-') ? show : `ion-${show}`;
    const base = folderOf(tag, newTree) ?? folderOf(tag, oldTree);
    if (!base) { console.error(`no component "${show}" in either version`); process.exit(1); }
    const empty = path.join(cache, 'empty');
    if (!fs.existsSync(empty)) fs.writeFileSync(empty, '');
    for (const ext of ['.js', '.md.css', '.css']) {
        const a = path.join(oldTree, base + ext);
        const b = path.join(newTree, base + ext);
        if (!fs.existsSync(a) && !fs.existsSync(b)) continue;
        process.stdout.write(gitDiff(fs.existsSync(a) ? a : empty, fs.existsSync(b) ? b : empty));
    }
    process.exit(0);
}

if (flag('--checklists')) {
    // ionic-api.js reads node_modules, so the checklists are made here from each version's core.json,
    // by running it against a folder that holds only that file.
    const checklist = (tree, tag) => {
        const stage = path.join(cache, `stage-${path.basename(tree)}`);
        const docsDir = path.join(stage, 'node_modules', '@ionic', 'docs');
        if (!fs.existsSync(path.join(stage, 'tools', 'ionic-api.js'))) {
            fs.mkdirSync(docsDir, { recursive: true });
            fs.mkdirSync(path.join(stage, 'tools'), { recursive: true });
            fs.copyFileSync(path.join(tree, 'core.json'), path.join(docsDir, 'core.json'));
        }
        // Always this checkout's script, so that only the data differs.
        fs.copyFileSync(path.join(__dirname, 'ionic-api.js'), path.join(stage, 'tools', 'ionic-api.js'));
        const r = spawnSync(process.execPath, [path.join(stage, 'tools', 'ionic-api.js'), tag], { encoding: 'utf8' });
        return r.status === 0 ? r.stdout : '';
    };
    const tags = flag('--all')
        ? [...new Set([...oldCore.components, ...newCore.components].map(c => c.tag))].sort()
        : avonicTagsWithFamilies();
    let any = false;
    for (const tag of tags) {
        const a = path.join(cache, `checklist-old.md`);
        const b = path.join(cache, `checklist-new.md`);
        fs.writeFileSync(a, checklist(oldTree, tag));
        fs.writeFileSync(b, checklist(newTree, tag));
        const out = gitDiff(a, b, ['--unified=1']);
        if (!out.trim()) continue;
        any = true;
        console.log(`\n## ${tag}\n`);
        console.log(out.split('\n').filter(l => /^[-+@ ]/.test(l) && !/^(---|\+\+\+) /.test(l)).join('\n'));
    }
    if (!any) console.log(`No checklist differs between ${oldVersion} and ${newVersion}.`);
    process.exit(0);
}

/** Avonic's tags, and the tags documented in another's folder (ion-row in ion-grid, ion-radio-group in ion-radio). */
function avonicTagsWithFamilies() {
    const all = new Set(newCore.components.map(c => c.tag).concat(oldCore.components.map(c => c.tag)));
    const tags = new Set(avonicTags.filter(t => all.has(t)));
    const claude = fs.readFileSync(path.join(repo, 'CLAUDE.md'), 'utf8');
    for (const m of claude.matchAll(/`(ion-[a-z-]+)`/g)) if (all.has(m[1])) tags.add(m[1]);
    // And a tag named after one of those: ion-card-header, in ion-card's docs.
    const own = [...tags];
    for (const tag of all) {
        if (!/legacy/.test(tag) && own.some(t => tag.startsWith(`${t}-`))) tags.add(tag);
    }
    return [...tags].sort();
}

// ---- The summary ----
const tags = flag('--all')
    ? [...new Set([...oldCore.components, ...newCore.components].map(c => c.tag))].sort()
    : avonicTagsWithFamilies();

console.log(`# Ionic ${oldVersion} → ${newVersion}${flag('--all') ? '' : ' (Avonic\'s components)'}\n`);
console.log('Changed lines after normalising (added + removed). `-` is no such file.\n');
console.log('| Component | js | md.css | css |');
console.log('|---|---|---|---|');
const cell = n => n === null ? '-' : String(n);
let unchanged = [];
for (const tag of tags) {
    const base = folderOf(tag, newTree) ?? folderOf(tag, oldTree);
    if (!base) { console.log(`| ${tag} | (no files) | | |`); continue; }
    const counts = ['.js', '.md.css', '.css'].map(ext =>
        changedLines(path.join(oldTree, base + ext), path.join(newTree, base + ext)));
    if (counts.every(n => !n)) { unchanged.push(tag); continue; }
    console.log(`| ${tag} | ${counts.map(cell).join(' | ')} |`);
}
console.log(`\nUnchanged: ${unchanged.join(', ') || 'none'}.\n`);

// Global CSS and utils, file by file.
for (const [title, folder] of [['Global CSS', 'css'], ['utils', 'utils']]) {
    const out = gitDiff(path.join(oldTree, folder), path.join(newTree, folder), ['--numstat']);
    console.log(`## ${title}\n`);
    if (!out.trim()) { console.log('No change.\n'); continue; }
    for (const line of out.trim().split('\n')) {
        const [added, removed, ...rest] = line.split('\t');
        // git quotes a path and doubles its backslashes: `"a/{tree-8.8.1 => tree-8.8.19}/css/x.css"`.
        const file = rest.join(' ').replace(/\\+/g, '/').replace(/"/g, '').replace(/.*tree-[^/]+\}?\/+/, '');
        console.log(`- \`${file}\`: +${added} −${removed}`);
    }
    console.log('');
}

// The API, from core.json.
console.log('## API (core.json)\n');
const byTag = core => new Map(core.components.map(c => [c.tag, c]));
const oldTags = byTag(oldCore);
const newTags = byTag(newCore);
const lines = [];
for (const tag of new Set([...oldTags.keys(), ...newTags.keys()])) {
    const a = oldTags.get(tag);
    const b = newTags.get(tag);
    if (!a) { lines.push(`- **${tag}**: added`); continue; }
    if (!b) { lines.push(`- **${tag}**: removed`); continue; }
    const kinds = [
        ['prop', c => c.props, p => p.name, p => `${p.type} = ${p.default}${p.deprecation !== undefined ? ' (deprecated)' : ''}`],
        ['event', c => c.events, e => e.event, e => e.detail],
        ['method', c => c.methods, m => m.name, m => m.signature],
        ['slot', c => c.slots, s => s.name || '(default)', () => ''],
        ['part', c => c.parts, p => p.name, () => ''],
        ['css var', c => c.styles.filter(s => s.mode !== 'ios'), s => s.name, () => ''],
    ];
    for (const [kind, list, key, value] of kinds) {
        const before = new Map(list(a).map(x => [key(x), value(x)]));
        const after = new Map(list(b).map(x => [key(x), value(x)]));
        for (const [k, v] of after) {
            if (!before.has(k)) lines.push(`- **${tag}** ${kind} \`${k}\`: added${v ? ` (\`${v}\`)` : ''}`);
            else if (before.get(k) !== v) lines.push(`- **${tag}** ${kind} \`${k}\`: \`${before.get(k)}\` → \`${v}\``);
        }
        for (const k of before.keys()) if (!after.has(k)) lines.push(`- **${tag}** ${kind} \`${k}\`: removed`);
    }
    if ((a.deprecation !== undefined) !== (b.deprecation !== undefined)) lines.push(`- **${tag}**: deprecation changed`);
}
console.log(lines.length ? lines.join('\n') : 'No change.');
