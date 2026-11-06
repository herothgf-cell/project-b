import {readFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';
const path=process.argv[2];
const profile=JSON.parse(readFileSync(path,'utf8'));
const values=[...profile.values].sort((a,b)=>a.id<b.id?-1:a.id>b.id?1:0);
const material=`content-profile-v${profile.schemaVersion}\n${profile.id}\n${profile.revision}\n`+values.map(({id,value})=>`${id}\t${Buffer.byteLength(value,'utf8')}:${value}\n`).join('');
assert.equal(createHash('sha256').update(material,'utf8').digest('hex'),profile.contentHash);
assert(profile.values.some(v=>v.value.includes('한글\n"é😀')),'Fixture must include newline, quote, non-ASCII and astral Unicode');
console.log(`CONTENT_UTF8_CSHARP_NODE_HASH_OK ${profile.contentHash}`);

const {migrateProfile}=await import('../tools/content-editor/profile.mjs');const catalog=JSON.parse(readFileSync(new URL('../config/content/catalog.json',import.meta.url),'utf8'));const legacy=JSON.parse(readFileSync(path+'.legacy.json','utf8'));const migrated=JSON.parse(readFileSync(path+'.migrated.json','utf8'));assert.equal(migrateProfile(legacy,catalog).contentHash,migrated.contentHash);console.log('CONTENT_MIGRATION_CSHARP_NODE_HASH_OK '+migrated.contentHash);
