"""Planar geometry helpers shared by the Eungye GIS importer."""


def clip_segment(a,b,box):
    dx,dy=b[0]-a[0],b[1]-a[1]
    lo,hi=0.,1.
    for p,q in zip((-dx,dx,-dy,dy),(a[0]-box[0],box[2]-a[0],a[1]-box[1],box[3]-a[1])):
        if abs(p)<1e-12:
            if q<0:return None
        elif p<0:lo=max(lo,q/p)
        else:hi=min(hi,q/p)
    if lo>=hi:return None
    return ((a[0]+lo*dx,a[1]+lo*dy),(a[0]+hi*dx,a[1]+hi*dy))

def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])

def triangulate(points):
    """Ear clipping for a simple ring; fail rather than invent a roof."""
    area=sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(points,points[1:]+points[:1]))
    ids=list(range(len(points)))
    if area<0:ids.reverse()
    triangles=[]
    while len(ids)>3:
        for i,b in enumerate(ids):
            a,c=ids[i-1],ids[(i+1)%len(ids)]
            if cross(points[a],points[b],points[c])<=1e-8:continue
            if any(all(cross(points[u],points[v],points[j])>=-1e-8 for u,v in [(a,b),(b,c),(c,a)]) for j in ids if j not in (a,b,c)):continue
            triangles.extend((a,b,c));ids.pop(i);break
        else:raise ValueError('Non-simple or degenerate polygon')
    if len(ids)==3:triangles.extend(ids)
    return triangles

def densify(points):
    import math
    result=[points[0]]
    for a,b in zip(points,points[1:]):
        count=max(1,math.ceil(math.dist(a,b)/15))
        result.extend((a[0]+(b[0]-a[0])*i/count,a[1]+(b[1]-a[1])*i/count) for i in range(1,count+1))
    return result
