import {readFile} from 'node:fs/promises';
import {parseJson,validateProfile} from './profile.mjs';
try {
 const [catalogPath,profilePath]=process.argv.slice(2);
 if(!catalogPath||!profilePath)throw Error('카탈로그와 프로필 경로가 필요합니다.');
 const raw=await readFile(profilePath);
 if(raw.length>1024*1024)throw Error('프로필 크기 제한 초과');
 const catalog=parseJson(await readFile(catalogPath,'utf8'));
 const profile=parseJson(raw.toString('utf8'));
 const errors=validateProfile(profile,catalog);
 if(errors.length)throw Error(errors.join('\n'));
 console.log('CONTENT_PROFILE_VALID '+profile.contentHash);
}catch(error){console.error(error.message);process.exitCode=1;}
