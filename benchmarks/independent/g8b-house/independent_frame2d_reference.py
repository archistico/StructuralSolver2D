#!/usr/bin/env python3
"""G8B reproducible independent first-order Euler-Bernoulli 2D FEM reference.

Requires numpy. Reads G8B JSON from project root. Not used by the production solver;
not a normative member check, not a wind/snow calibration or certified benchmark.
"""
import json
from pathlib import Path
import math
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
MODELS=ROOT/'examples/mixed/g8b-house'
COMBINATIONS=('ULS_GRAVITY','ULS_WIND_POS','ULS_WIND_NEG','SLS_RARE')


def analyze(model, combo):
    nodes={item['id']:item for item in model['nodes']}
    node_idx={key:i for i,key in enumerate(nodes)}
    mats={item['id']:item for item in model['materials']}
    secs={item['id']:item for item in model['sections']}
    members={item['id']:item for item in model['members']}
    load_factors={t['loadCaseId']:t['factor'] for c in model['loadCombinations'] if c['id']==combo for t in c['terms']}
    n=len(nodes)
    K=np.zeros((3*n,3*n),dtype=float)
    F=np.zeros(3*n,dtype=float)
    Fmom=np.zeros(3,dtype=float) # applied Fx, Fy, total M about (0,0)
    member_data={}

    def add_member_load(mid,q):
        e=member_data[mid]
        a,b=e['a'],e['b'];L,c,s=e['L'],e['c'],e['s']
        if e['type']=='Truss2D':
            for nid in (a,b):
                F[3*node_idx[nid]+1]+=q*L/2
        else:
            # direction GlobalY -> local vector (q*sin(theta), q*cos(theta))
            qx=q*s; qy=q*c
            f_local=np.array([qx*L/2,qy*L/2,qy*L*L/12,qx*L/2,qy*L/2,-qy*L*L/12])
            f_global=e['T'].T@f_local
            F[e['dofs']]+=f_global
        Fmom[1]+=q*L
        Fmom[2]+=q*L*(nodes[a]['x']+nodes[b]['x'])/2

    for mid,m in members.items():
        a=m['startNodeId'];b=m['endNodeId']
        dx=nodes[b]['x']-nodes[a]['x']; dy=nodes[b]['y']-nodes[a]['y']
        L=math.hypot(dx,dy)
        c,s=dx/L,dy/L
        sec=secs[m['sectionId']]; mat=mats[m['materialId']]
        E=mat['elasticModulus'];A=sec['area'];I=sec['momentOfInertia']
        dofs=np.array([3*node_idx[a]+z for z in range(3)]+[3*node_idx[b]+z for z in range(3)])
        if m['type']=='Frame2D':
            EA=E*A/L; EI=E*I
            kb=12*EI/L**3; kc=6*EI/L**2; kd=4*EI/L; ke=2*EI/L
            kl=np.array([
             [ EA, 0,0,-EA,0,0],
             [0,kb,kc,0,-kb,kc],
             [0,kc,kd,0,-kc,ke],
             [-EA,0,0,EA,0,0],
             [0,-kb,-kc,0,kb,-kc],
             [0,kc,ke,0,-kc,kd]])
            t=np.array([[c,s,0],[-s,c,0],[0,0,1]])
            T=np.zeros((6,6));T[:3,:3]=t;T[3:,3:]=t
            kg=T.T@kl@T
        elif m['type']=='Truss2D':
            T=None
            ee=E*A/L
            tt=np.array([c,s,0,-c,-s,0])
            kg=ee*np.outer(tt,tt)
        else:raise ValueError(m['type'])
        K[np.ix_(dofs,dofs)]+=kg
        member_data[mid]={'a':a,'b':b,'L':L,'c':c,'s':s,'T':T,'dofs':dofs,'type':m['type'],'mat':mat,'sec':sec}

    for load in model['loads']:
        factor=load_factors.get(load['loadCaseId'],0)
        if factor==0:continue
        typ=load['type'];direction=load['direction'];val=load['value']*factor
        if typ=='SelfWeight':
            for mid,e in member_data.items():
                q=-val*e['mat']['unitWeight']*e['sec']['area']
                add_member_load(mid,q)
        elif typ=='UniformDistributedLoad':
            assert direction=='GlobalY'
            add_member_load(load['targetId'],val)
        elif typ=='NodalForce':
            ni=3*node_idx[load['targetId']]
            if direction=='GlobalX':
                F[ni]+=val
                Fmom[0]+=val
                Fmom[2]-=val*nodes[load['targetId']]['y']
            elif direction=='GlobalY':
                F[ni+1]+=val
                Fmom[1]+=val
                Fmom[2]+=val*nodes[load['targetId']]['x']
            else:raise ValueError(direction)
        else:raise ValueError(typ)
    fixed=[]
    for support in model['supports']:
        i=3*node_idx[support['nodeId']]
        fixed.extend(i+j for j,k in enumerate(('restrainedUx','restrainedUy','restrainedRz')) if support.get(k,False))
    fixed=np.array(sorted(set(fixed)),dtype=int)
    free=np.array([i for i in range(3*n) if i not in set(fixed)],dtype=int)
    u=np.zeros(3*n)
    u[free]=np.linalg.solve(K[np.ix_(free,free)],F[free]); R=K@u-F
    total_rx=sum(R[3*node_idx[s['nodeId']]] for s in model['supports'])
    total_ry=sum(R[3*node_idx[s['nodeId']]+1] for s in model['supports'])
    rm=sum(R[3*node_idx[s['nodeId']]+2] + nodes[s['nodeId']]['x']*R[3*node_idx[s['nodeId']]+1]-nodes[s['nodeId']]['y']*R[3*node_idx[s['nodeId']]] for s in model['supports'])
    unbalanced=np.array([total_rx+Fmom[0],total_ry+Fmom[1],rm+Fmom[2]])
    assert np.max(np.abs(unbalanced))<5e-6,unbalanced
    u_top={nid:float(u[3*node_idx[nid]])*1000 for nid in ('A2','B2','C2','D2','RIDGE')}
    u_floor={nid:float(u[3*node_idx[nid]+1])*1000 for nid in ('A1','B1','C1','D1')}
    R_support={item['nodeId']:{'Rx_kN':float(R[3*node_idx[item['nodeId']]]),'Ry_kN':float(R[3*node_idx[item['nodeId']]+1]),'Mz_kNm':float(R[3*node_idx[item['nodeId']]+2])} for item in model['supports']}
    return dict(analysis='linear elastic first-order 2D independent reference',combination=combo,
         topUx_mm=u_top, floorUy_mm=u_floor, maxAbsUx_mm=float(np.max(np.abs(u[::3]))*1000),maxAbsUy_mm=float(np.max(np.abs(u[1::3]))*1000),
         globalLoads=dict(Fx_kN=float(Fmom[0]),Fy_kN=float(Fmom[1]),Mz_kNm=float(Fmom[2])),
         supportReactions=R_support,globalEquilibriumResidual=[float(x) for x in unbalanced])


def main():
    out={'schemaVersion':1,'status':'Independent exploratory elastic 2D calculation; comparison values NOT verified against StructuralSolver2D on Windows','models':[]}
    for path in sorted(p for p in MODELS.glob('g8b-*.json') if p.name.endswith(('-controventato.json','-senza-controventi.json'))):
        model=json.loads(path.read_text())
        entries=[analyze(model,c) for c in COMBINATIONS]
        out['models'].append({'file':path.name,'combinations':entries})
        wind=next(e for e in entries if e['combination']=='ULS_WIND_POS')
        print(f"{path.name:52s} | max ux wind {wind['maxAbsUx_mm']:10.4f} mm; ridge ux {wind['topUx_mm']['RIDGE']:10.4f} mm")
    dest=Path(__file__).resolve().parent/'g8b-risultati-elastici-indipendenti.json'
    dest.write_text(json.dumps(out,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print('Wrote',dest)
if __name__=='__main__':main()
