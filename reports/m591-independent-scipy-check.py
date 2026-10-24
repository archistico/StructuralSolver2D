# Independent audit reference; optional Python 3 + NumPy + SciPy.
# Models the supplied M59 4-m cantilever and compares three Newmark time grids.
# This is NOT the .NET solver nor a replacement for running xUnit on Windows.
import numpy as np
from scipy.linalg import eigh
E=210_000_000.0; A=0.01; I=1e-4; gamma=77.0; grav=9.80665
mu=gamma*A/grav
l=.5;nn=9;N=nn*3
K=np.zeros((N,N));M=np.zeros((N,N))
kb=E*I/l**3*np.array([[12,6*l,-12,6*l],[6*l,4*l*l,-6*l,2*l*l],[-12,-6*l,12,-6*l],[6*l,2*l*l,-6*l,4*l*l]])
mb=mu*l/420*np.array([[156,22*l,54,-13*l],[22*l,4*l*l,13*l,-3*l*l],[54,13*l,156,-22*l],[-13*l,-3*l*l,-22*l,4*l*l]])
for i in range(nn-1):
 d=[3*i,3*i+1,3*i+2,3*i+3,3*i+4,3*i+5]
 kl=np.zeros((6,6));ml=np.zeros((6,6))
 ix=[0,3];kl[np.ix_(ix,ix)]+=E*A/l*np.array([[1,-1],[-1,1]]);ml[np.ix_(ix,ix)]+=mu*l/6*np.array([[2,1],[1,2]])
 ix=[1,2,4,5];kl[np.ix_(ix,ix)]+=kb;ml[np.ix_(ix,ix)]+=mb
 K[np.ix_(d,d)]+=kl;M[np.ix_(d,d)]+=ml
v,P=eigh(K[3:,3:],M[3:,3:]);freq=np.sqrt(v)/(2*np.pi)
print('Independent frequency range',freq[0],freq[-1]);print('first 8:',freq[:8]);dt=.002;duration=.6;print('dt*f',dt*freq[-1],(dt/4)*freq[-1]);print('Smallest dt for <=0.1',.1/freq[-1],'minimum steps for 0.6 seconds',duration*freq[-1]/.1)
# load pulse 0,.01,.03,.04,.6. Exact piecewise linear load for all grids
Nfree=len(v);s=P.T@np.eye(len(v))[:,-2] * -1 # free Uy of N8 is local global index -2
# modal ratio .03, modal stiffness lambda; first all N modes

def simulate(dt):
    steps=int(round(duration/dt)); q=np.zeros(Nfree); vel=np.zeros(Nfree);a=np.zeros(Nfree);c=.06*np.sqrt(v)
    def factor(t):return np.interp(t,[0,.01,.03,.04,.6],[0,1,1,0,0])
    out=[]
    for tix in range(steps+1):
        t=tix*dt; f=s*factor(t)
        if tix==0:a=f-c*vel-v*q
        else:
            up=q+dt*vel+dt*dt*.25*a;vp=vel+.5*dt*a
            an=(f-c*vp-v*up)/(1+.5*dt*c+.25*dt*dt*v)
            q=up+.25*dt*dt*an;vel=vp+.5*dt*an;a=an
        out.append((q@P[-2,:],vel@P[-2,:],a@P[-2,:]))
    return np.array(out)
C=simulate(.002);H=simulate(.001);F=simulate(.0005)
print("independent coarse Uy envelope",max(abs(C[:,0])),"finest Uy envelope",max(abs(F[:,0])))
for i,label in enumerate(['Uy','Vy','Ay']):
    cm=np.max(np.abs(C[:,i]-H[::2,i]));mf=np.max(np.abs(H[:,i]-F[::2,i]));den=np.max(abs(F[:,i]));print(label,'peak',den,'C/H',cm/den,'H/F',mf/den,'order',np.log2(cm/mf),'peak times:',np.argmax(abs(C[:,i]))*.002,np.argmax(abs(H[:,i]))*.001,np.argmax(abs(F[:,i]))*.0005)
