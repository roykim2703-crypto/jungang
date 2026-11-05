const fs=require('fs'),crypto=require('crypto');
const font='6ca441b1906b526468aa944bdb56b07e', material='-4017394534332255502', rounded='4f5840c9eefeeec4399ac39ab898fe3e';
const files=[...fs.readdirSync('Assets/Scenes').filter(x=>x.endsWith('.unity')).map(x=>'Assets/Scenes/'+x),...fs.readdirSync('Assets/Prefeb').filter(x=>x.endsWith('.prefab')).map(x=>'Assets/Prefeb/'+x)];
const panels=new Set(['RewardPanel','사이드 탭','상단 배젤','배경','surrenrder','surrender Background','GetTime','카드','그림','gibo','BlackCards','WhiteCards','MoveHistoryViewport','RewardGaugeTrack']);
for(const path of files){
 let original=fs.readFileSync(path,'utf8'),s=original.replace(/\r\n/g,'\n');
 s=s.replace(/m_fontAsset: \{[^\n]+\}/g,`m_fontAsset: {fileID: 11400000, guid: ${font}, type: 2}`).replace(/m_sharedMaterial: \{[^\n]+\}/g,`m_sharedMaterial: {fileID: ${material}, guid: ${font}, type: 2}`);
 s=s.replace(/m_Font: \{[^\n]+\}/g,'m_Font: {fileID: 12800000, guid: aabb2bfe776bb9c439e0a0dcc2cc4838, type: 3}');
 let blocks=s.split(/(?=^--- !u!)/m), gos=new Map(),images=new Map(),targets=new Set(),already=new Set();
 for(const b of blocks){let h=b.match(/^--- !u!(\d+) &(-?\d+)/); if(!h)continue; let go=b.match(/  m_GameObject: \{fileID: (-?\d+)\}/)?.[1];
 if(h[1]==='1'){let n=b.match(/^  m_Name: (.*)$/m)?.[1];if(n?.startsWith('"'))n=JSON.parse(n);gos.set(h[2],{name:n,index:blocks.indexOf(b)});}
 if(b.includes('  m_Sprite:')) images.set(h[2],go);
 if(b.includes('guid: '+rounded))already.add(go);
 let target=b.match(/m_TargetGraphic: \{fileID: (-?\d+)\}/)?.[1];if(target)targets.add(target);
 }
 let next=7900000000,added=0;
 for(const [id,go] of images){let obj=gos.get(go);if(!obj||already.has(go)||(!targets.has(id)&&!panels.has(obj.name)))continue;
 while(s.includes('&'+next))next++;
 let component=String(next++);blocks[obj.index]=blocks[obj.index].replace('  m_Component:\n','  m_Component:\n  - component: {fileID: '+component+'}\n');
 blocks.push(`--- !u!114 &${component}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${go}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${rounded}, type: 3}\n  m_Name:\n  m_EditorClassIdentifier: Assembly-CSharp::RoundedUI\n  radius: ${obj.name==='RewardPanel'?28:18}\n`);added++;
 }
 fs.writeFileSync(path,blocks.join(''));console.log(path+': '+added+' rounded surfaces');
}
for(const path of ['Assets/Resources/MultiplayerMenuAssets.asset','Assets/TextMesh Pro/Resources/TMP Settings.asset']){
 let s=fs.readFileSync(path,'utf8');s=s.replace(/(  (?:Font|m_defaultFontAsset): )\{[^\r\n]+\}/g,`$1{fileID: 11400000, guid: ${font}, type: 2}`);fs.writeFileSync(path,s);
}
const fp='Assets/Fonts/서울남산 장체L SDF.asset';fs.writeFileSync(fp,fs.readFileSync(fp,'utf8').replace('m_IsMultiAtlasTexturesEnabled: 0','m_IsMultiAtlasTexturesEnabled: 1'));
const meta='Assets/Editor/UiThemeSetup.cs.meta';if(!fs.existsSync(meta))fs.writeFileSync(meta,'fileFormatVersion: 2\nguid: '+crypto.randomBytes(16).toString('hex')+'\n');
