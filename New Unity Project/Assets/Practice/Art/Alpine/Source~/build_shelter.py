"""Rebuild the North Ridge set from Kenney Nature Kit 2.1. Pass the original ZIP as argv[1]."""
from collections import defaultdict
from functools import lru_cache
from math import cos, sin, pi
from pathlib import Path
import random
import sys
import zipfile

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
KIT = zipfile.ZipFile(sys.argv[1])
RNG = random.Random(1804)
# Each swatch occupies a full block in the shared texture, so all five scene
# sections batch by material. Separate meshes allow independent editing/culling.
COLORS = {
    "ice":"bccfd7","snow":"e4e9e7","sunSnow":"f0eee4","shade":"a5bfca",
    "trail":"9faeb0","track":"839799","deep":"688c9e","peak":"93a9b1","peakShade":"728b97",
    "stone":"667b80","stoneLight":"85999d","stoneDark":"495c61","scree":"aebdc0",
    "spruce":"304f4c","pine":"46645c","blueSpruce":"57736b","pineShadow":"314742",
    "bark":"51473f","trunk":"746456","lumber":"ab7753","grain":"936546","honey":"c29167",
    "cut":"c5a079","dry":"b68b65","sage":"4d6860","sageLight":"60766a","deepSage":"3a554f",
    "red":"b95e38","ochre":"dab372","brass":"bfa878","metal":"49595b","lightMetal":"7d8581",
    "lamp":"ffe2a0","glass":"8ba9a5","cream":"e1d7b8","ink":"31423e","wool":"ae795b"
}
SWATCH = {k:i for i,k in enumerate(COLORS)}

def unit(v):
    return v / max(np.linalg.norm(v), 1e-9)

class Mesh:
    def __init__(self):
        self.faces=[]
    def face(self, pts, color, toward=None):
        p=np.array(pts,dtype=float)
        n=np.cross(p[1]-p[0], p[2]-p[0])
        if np.linalg.norm(n)<1e-9:return
        if toward is not None and np.dot(n,toward)<0:p=p[::-1]
        self.faces.append((p,color))
    def box(self, center, size, color, rotation=None):
        basis=np.eye(3) if rotation is None else rotation
        c=np.array(center,dtype=float)
        for ax in range(3):
            a,b=(ax+1)%3,(ax+2)%3
            for sign in [-1,1]:
                pts=[]
                for j,k in [(-1,-1),(1,-1),(1,1),(-1,1)]:
                    q=np.zeros(3);q[ax]=sign*size[ax]/2;q[a]=j*size[a]/2;q[b]=k*size[b]/2
                    pts.append(c+basis@q)
                self.face(pts,color,sign*basis[:,ax])
    def beam(self, start, end, width, depth, color):
        a,b=np.array(start),np.array(end)
        y=unit(b-a); ref=np.array((0,0,1)) if abs(y[2])<.9 else np.array((1,0,0))
        x=unit(np.cross(y,ref)); z=unit(np.cross(x,y))
        self.box((a+b)/2,(width,np.linalg.norm(b-a),depth),color,np.column_stack((x,y,z)))
    def save(self,name):
        # Unity imports OBJ in right-handed file space with mirrored X.
        lines=["# North Ridge, FIRST180. See Credits.txt.","o "+name]
        tri=[]; vertex_ids={}; normals={}; uv={}
        for face,color in self.faces:
            for i in range(1,len(face)-1):
                points=face[[0,i,i+1]]
                normal=unit(np.cross(points[1]-points[0],points[2]-points[0]))
                norm=tuple(np.round(normal*np.array((-1,1,1)),5))
                ni=normals.setdefault(norm,len(normals)+1)
                ti=uv.setdefault(color,len(uv)+1); row=[]
                for pt in points[::-1]:
                    key=tuple(np.round(pt*np.array((-1,1,1)),5))
                    vi=vertex_ids.setdefault(key,len(vertex_ids)+1)
                    row.append(f"{vi}/{ti}/{ni}")
                tri.append("f "+" ".join(row))
        lines.extend("v %.5f %.5f %.5f"%xyz for xyz in vertex_ids)
        lines.extend("vn %.5f %.5f %.5f"%xyz for xyz in normals)
        for color in uv:
            n=SWATCH[color];lines.append("vt %.6f %.6f"%((n%8+.5)/8,1-(n//8+.5)/8))
        with (ROOT/(name+".obj")).open('w',newline='\n') as fh:fh.write('\n'.join(lines+tri)+'\n')
        print(name,len(tri),"triangles")

@lru_cache(None)
def kit_model(name):
    raw=KIT.read("Models/OBJ format/"+name+".obj").decode().splitlines()
    v=[];n=[];faces=[];mat=''
    for l in raw:
        s=l.split()
        if not s:continue
        if s[0]=='v':v.append([float(k) for k in s[1:4]])
        elif s[0]=='vn':n.append([float(k) for k in s[1:4]])
        elif s[0]=='usemtl':mat=s[1]
        elif s[0]=='f':
            ids=[w.split('/') for w in s[1:]]
            face=np.array([v[int(w[0])-1] for w in ids])
            if np.dot(np.cross(face[1]-face[0],face[2]-face[0]), n[int(ids[0][-1])-1])<0:face=face[::-1]
            faces.append((face,mat))
    return faces

def snow_cap(p,height):
    q=[]
    for a,b in zip(p,np.roll(p,-1,axis=0)):
        if a[1]>=height:q.append(a)
        if (a[1]>=height)!=(b[1]>=height):
            q.append(a+(b-a)*((height-a[1])/(b[1]-a[1])))
    return q

def place_kit(mesh,name,pos,scale,angle):
    matrix=np.array([[cos(angle),0,-sin(angle)],[0,1,0],[sin(angle),0,cos(angle)]])
    for raw,material in kit_model(name):
        p=(raw*np.array(scale))@matrix.T+np.array(pos)
        n=unit(np.cross(p[1]-p[0],p[2]-p[0]))
        bark='wood' in material
        leaf='leaf' in material
        col="bark" if bark else ("pine" if leaf else "stone")
        if "Inner" in material:col="cut"
        if leaf and n[0]>.2:col="spruce"
        if not leaf and not bark and n[0]>.3:col="stoneLight"
        mesh.face(p,col)
        if not bark and n[1]>.06 and np.ptp(p[:,1])>.012:
            lo=min(p[:,1]); threshold=lo+np.ptp(p[:,1])*(.27+max(n[0],0)*.23)
            cap=snow_cap(p,threshold)
            if len(cap)>=3:mesh.face(np.array(cap)+n*.008,"snow" if n[0]<.45 else "ice")

def terrain():
    mesh=Mesh()
    # Continuous snow field with a flat maintained clearing and low drifted perimeter.
    xs=np.arange(-47,48,2.5);zs=np.arange(-36,60,2.5)
    points={}
    for ix,x in enumerate(xs):
        for iz,z in enumerate(zs):
            xx=x+RNG.uniform(-.48,.48);zz=z+RNG.uniform(-.48,.48)
            # Level the route all the way to the south overlook. Keep the drifts outside its ropes.
            edge=max(abs(xx)-(11.5 if zz<-6 else 8),zz-8,-zz-28.5,0)
            h=-.025+min(1,edge/5)*(.23+.28*cos(xx*.36)+.18*sin(zz*.52)+.22*RNG.random())
            points[ix,iz]=(xx,h,zz)
    for i in range(len(xs)-1):
        for j in range(len(zs)-1):
            a,b,c,d=[points[k] for k in [(i,j),(i+1,j),(i+1,j+1),(i,j+1)]]
            col="snow" if max(abs(a[0]),abs(a[2]))<10 else RNG.choice(["snow","snow","snow","ice"])
            mesh.face((a,b,d),col,(0,1,0));mesh.face((b,c,d),col,(0,1,0))
    # Shallow packed trail; the track branches at the waystone in front of the refuge.
    for path in [[(1,-9,1.8),(.2,-6,1.6),(-.6,-3.3,1.55),(-1.8,-1.4,1.55),(-3.3,.15,1.4),(-5,1.6,1.1)],
                 [(-.4,-3,1.35),(1.5,-1.9,1.15),(3.8,-.3,1.2),(5.2,1.7,1),(6.3,3,1)],
                 [(1,-9,1.8),(-.5,-11,1.5),(-3.3,-14.8,1.25),(-4.7,-19.2,1.2),(-3,-22.3,1.1),(0,-24,1.1)],
                 [(0,-24,1.1),(3.3,-21.5,1.2),(4.7,-17.2,1.2),(4.4,-14,1.15),(1,-9,1.55)]]:
        edges=[]
        for i,(x,z,w) in enumerate(path):
            a=path[max(0,i-1)];b=path[min(len(path)-1,i+1)]
            side=unit(np.array((b[1]-a[1],0,a[0]-b[0])))*w
            c=np.array((x,.008,z));edges.append((c-side,c+side))
        for (a,b),(c,d) in zip(edges,edges[1:]):mesh.face((a,b,d,c),"trail",(0,1,0))
    mesh.save("TrailClearing")

def mountain():
    mesh=Mesh()
    # Jagged alpine ridges wrap the site. Their foothills hide the perimeter of the snowfield.
    for radius,alt in [(60,18),(43,10)]:
        rows=[]
        for i in range(57):
            ang=i*2*pi/56
            crest=alt*(.62+.22*cos(ang*7.1)+.16*sin(ang*11.4)+.18*RNG.random())
            vec=np.array((cos(ang),0,sin(ang)))
            row=[]
            for rad,y in [(radius-12,-.7),(radius-6,crest*.37),(radius-2,crest*.76),(radius,crest)]:
                pt=vec*rad;pt[1]=y;row.append(pt)
            rows.append(row)
        rows[-1]=rows[0]
        for l,r in zip(rows,rows[1:]):
            for j in range(3):
                toward=-(l[j]+r[j]);toward[1]=10
                base=['shade','peak','sunSnow'][j]; edge=['ice','peakShade','snow'][j]
                mesh.face((l[j],r[j],l[j+1]),base,toward)
                mesh.face((r[j],r[j+1],l[j+1]),edge,toward)
    mesh.save("SawtoothRidges")

def forest():
    mesh=Mesh()
    # Fixed groves leave sightlines open through the care area and hut door.
    seeds=[(8,5), (11,8), (13,1), (9,15), (3,13), (-4,13), (-12,10), (-14,1),
           (-14,-7), (14,-8), (14,-17), (-14,-17), (0,20),(20,13),(-20,15),
           (-10,-30),(2,-31),(12,-30),(-15,-24),(15,-24)]
    for j,(x,z) in enumerate(seeds):
        for i in range(4 if j<8 else 3):
            xx=x+RNG.uniform(-2.5,2.5); zz=z+RNG.uniform(-2.8,2.8)
            if abs(xx)<10 and -8<zz<8 and not (xx>5 and zz>2.5):continue
            tall=RNG.uniform(2.3,4.6)
            model=RNG.choice(["tree_pineTallA_detailed","tree_pineRoundC","tree_pineSmallA"])
            place_kit(mesh,model,(xx,.04,zz),(tall*1.35,tall,tall*1.35),RNG.uniform(0,6))
    for (x,z,size) in [(-8.9,-1.6,1.2),(6.4,3,1),(5,6.4,1.8),(6,7.7,1.05),(9.1,.2,.85),(-9.4,7.1,1.4),(-8.4,8.9,1.6),(1.7,7.3,1.2),(3.4,10,1.6),(2.5,-8.4,.8)]:
        place_kit(mesh,"stone_largeA",(x,-.015,z),(size*1.7,size*1.8,size),RNG.uniform(0,6))
    # Rock outcrops fringe the extended overlook outside the ropes. Leave the
    # treatment lanes and the view of the shelter unobstructed.
    for x,z,size in [(-11.45,-13.4,1.45),(-11.08,-14.85,.81),(-10.98,-20.55,1.16),
                     (11.04,-12.55,.90),(11.55,-18.9,1.40),(11.22,-20.57,.85),
                     (-5.65,-27.89,1.62),(-3.71,-27.51,.87),(7.69,-27.45,1.06)]:
        place_kit(mesh,"stone_largeA",(x,-.05,z),(size*1.67,size*1.24,size),RNG.uniform(0,6))
    mesh.save("SnowlineForest")

def shelter():
    mesh=Mesh()
    # Plank deck reaches the existing 0.20 m handoff height. Wide east-facing
    # side window and central doorway admit light and preserve all treatment approaches.
    X=-5;front=1.48;back=6.74; left=-7.52;right=-2.48;eave=2.8;apex=4.12
    for i in range(26):
        x=left+i*.197+.097
        mesh.box((x,.10,4.02),(.193,.20,5.86),RNG.choice(["grain","lumber","dry"]))
    for x in (left,right):
        for z in (front,back):
            mesh.box((x,1.49,z),(.20,2.62,.20),"honey")
    for i in range(13):
        y=.30+i*.194
        mat=RNG.choice(["sage","sage","sageLight","deepSage"])
        mesh.box((X,y,back),(5.05,.186,.13),mat)
        for a,b in [(left,-6.31),(-3.70,right)]:
            mesh.box(((a+b)/2,y,front),(b-a,.184,.14),mat)
        if y>2.36: mesh.box((X,y,front),(2.63,.184,.14),mat)
        for x in (left,right):
            parts=[(front,back)] if y<1.12 or y>2.18 else [(front,2.2),(5.4,back)]
            for a,b in parts:mesh.box((x,y,(a+b)/2),(.14,.186,b-a),mat)
    for x in (left,right):
        mesh.box((x,1.11,3.80),(.21,.11,3.36),"honey")
        for z in (2.18,3.8,5.43):mesh.box((x,1.66,z),(.12,1.14,.095),"lumber")
    for x in (-6.32,-3.68):
        mesh.box((x,1.30,front-.09),(.15,2.28,.16),"cut")
    mesh.box((X,2.47,front-.10),(2.82,.17,.17),"honey")
    for z in (front,back):
        mesh.face([(left,eave,z),(right,eave,z),(X,apex,z)],"deepSage",(0,0,-1 if z==front else 1))
        mesh.beam((left-.26,eave-.13,z-.10),(X,apex+.03,z-.10),.16,.18,"lumber")
        mesh.beam((right+.26,eave-.13,z-.10),(X,apex+.03,z-.10),.16,.18,"lumber")
        mesh.box((X,eave-.04,z-.08),(5.36,.16,.18),"honey")
    # Both layers of the pitched roof are closed volumes with visible warm eaves.
    for x in (left-.41,right+.41):
        dz=back-front+1.05
        a=np.array((x,eave-.21,4.08)); b=np.array((X,apex+.03,4.08)); u=unit(b-a)
        normal=unit(np.cross(np.array((0,0,1)),u))
        if normal[1]<0:normal=-normal
        basis=np.column_stack((u,normal,np.array((0,0,1))))
        mesh.box((a+b)/2,(np.linalg.norm(b-a),.16,dz),"trunk",basis)
        mesh.box((a+b)/2+normal*.13,(np.linalg.norm(b-a)+.02,.18,dz+.06),"sunSnow",basis)
    # Furniture around the perimeter leaves the inside handoff point accessible.
    for x in (-6.86,-3.35):
        mesh.box((x,.46,6.05),(.13,.51,.42),"grain")
    mesh.box((X,.75,6.03),(4.03,.12,.55),"honey")
    for i in range(3):
        mesh.box((-6.1,.86+i*.068,6.02),(.64,.067,.41),["wool","cream","sage"][i])
    mesh.box((-3.55,3.53,5.90),(.32,1.43,.34),"metal")
    mesh.box((-3.55,4.25,5.90),(.47,.1,.47),"lightMetal")
    for x in (-6.62,-3.38):
        mesh.box((x,2.13,front-.10),(.28,.38,.18),"ink")
        mesh.box((x,2.13,front-.21),(.18,.27,.065),"lamp")
        mesh.box((x,2.35,front-.15),(.36,.08,.31),"metal")
    # Trail sign on the gable: route diamond and quiet forest-service sign panel.
    mesh.box((X,3.04,front-.13),(2.66,.50,.10),"ink")
    for x in (-6.22,-3.78):mesh.box((x,3.04,front-.20),(.04,.04,.016),"brass")
    mesh.box((.95,.55,-.85),(.16,1.10,.16),"trunk")
    mesh.box((.95,1.14,-.85),(.62,.27,.13),"sage")
    mesh.face([(.70,1.14,-.925),(.83,1.23,-.925),(.83,1.18,-.925),(1.17,1.18,-.925),(1.17,1.10,-.925),(.83,1.10,-.925),(.83,1.05,-.925)],"cream",(0,0,-1))
    mesh.save("NorthRidgeRefuge")

texture=Image.new("RGB",(64,64))
for name,code in COLORS.items():
    i=SWATCH[name];x=i%8*8;y=i//8*8
    texture.paste(tuple(bytes.fromhex(code)),(x,y,x+8,y+8))
texture.save(ROOT/"RefugePalette.png")
terrain();mountain();forest();shelter()

