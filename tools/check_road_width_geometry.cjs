const fs=require('fs');
const p=require('../config/maps/project/eungye.project.json'),d=p.document;
const inventory=require('../docs/content-design/road-survey/colored-inventory.json').corridors;
const pink=inventory.filter(c=>c.color==='pink');
function path(points){let a=[0];for(let i=1;i<points.length;i++)a.push(a.at(-1)+Math.hypot(points[i].x-points[i-1].x,points[i].z-points[i-1].z));return {points,length:a.at(-1),at(s){let i=a.findIndex(v=>v>s);if(i<1)i=points.length-1;let t=Math.max(0,Math.min(1,(s-a[i-1])/(a[i]-a[i-1]))),u=points[i-1],v=points[i],len=a[i]-a[i-1];return {x:u.x+(v.x-u.x)*t,z:u.z+(v.z-u.z)*t,dx:(v.x-u.x)/len,dz:(v.z-u.z)/len};}}}
function inside(q,poly){let v=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++)if((poly[i].z>q.z)!=(poly[j].z>q.z)&&q.x<(poly[j].x-poly[i].x)*(q.z-poly[i].z)/(poly[j].z-poly[i].z)+poly[i].x)v=!v;return v;}
const areas=d.scene.objects.filter(o=>o.enabled&&o.kind==='sidewalk'&&o.height>.04).map(o=>({...o,b:[Math.min(...o.points.map(p=>p.x)),Math.min(...o.points.map(p=>p.z)),Math.max(...o.points.map(p=>p.x)),Math.max(...o.points.map(p=>p.z))]}));
const lanes=d.lanes.filter(l=>!l.disabled&&!l.change&&l.points.length>1&&!l.id.startsWith('fuel:'));
const hits=[];let samples=0;
for(const l of lanes){let g=pink.findIndex(c=>c.sourceIds.some(id=>l.sourceId.split(',').includes(id)));let route=path(l.points);let near=areas.filter(a=>l.points.some(q=>q.x>a.b[0]-4&&q.x<a.b[2]+4&&q.z>a.b[1]-4&&q.z<a.b[3]+4));if(!near.length)continue;
 for(let at=2;at<route.length-2;at+=2){let q=route.at(at);samples++;for(let offset of [-1.4,0,1.4]){let r={x:q.x+q.dz*offset,z:q.z-q.dx*offset};for(let a of near)if(r.x>=a.b[0]&&r.x<=a.b[2]&&r.z>=a.b[1]&&r.z<=a.b[3]&&inside(r,a.points))hits.push({group:g<0?'other':'P'+String(g+1).padStart(2,'0'),lane:l.id,turn:l.turn,x:+r.x.toFixed(2),z:+r.z.toFixed(2),offset,area:a.id});}}
}
const counts={};for(let h of hits){let k=h.group+' '+h.lane+' '+h.area;counts[k]=(counts[k]||0)+1;}
fs.writeFileSync('artifacts/color-road-survey/pink-width-audit.json',JSON.stringify({revision:d.revision,samples,hits},null,2));
console.log('samples',samples,'hits',hits.length);console.log(Object.entries(counts).sort((a,b)=>b[1]-a[1]).slice(0,40));
function distance(q,pts){let best=Infinity;for(let i=1;i<pts.length;i++){let a=pts[i-1],b=pts[i],dx=b.x-a.x,dz=b.z-a.z,t=Math.max(0,Math.min(1,((q.x-a.x)*dx+(q.z-a.z)*dz)/(dx*dx+dz*dz)));best=Math.min(best,Math.hypot(q.x-a.x-t*dx,q.z-a.z-t*dz));}return best;}
const roads=d.scene.objects.filter(o=>o.enabled&&o.kind==='road');const unsupported=[];let roadSamples=0;
for(const l of lanes.filter(l=>!l.turn&&!l.id.startsWith('join:'))){let route=path(l.points);let near=roads.filter(r=>r.points.some(p=>l.points.some(q=>Math.hypot(p.x-q.x,p.z-q.z)<r.width/2+30)));for(let at=3;at<route.length-3;at+=3){let q=route.at(at);for(let offset of [-1.4,1.4]){let r={x:q.x+q.dz*offset,z:q.z-q.dx*offset};roadSamples++;let margin=Math.max(...near.map(o=>o.width/2-distance(r,o.points)));if(margin<-.15)unsupported.push({lane:l.id,x:+r.x.toFixed(2),z:+r.z.toFixed(2),margin:+margin.toFixed(2)});}}}
fs.writeFileSync('artifacts/color-road-survey/road-width-support.json',JSON.stringify({roadSamples,unsupported},null,2));console.log('roadSamples',roadSamples,'unsupported',unsupported.length,unsupported.slice(0,15));
