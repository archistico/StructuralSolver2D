"""Standalone M60 one-bar consistent-mass validation (no project implementation imported)."""
import math
import numpy as np
from scipy.linalg import eigh

E=210_000_000.0; A=.01; L=2.0; unit_weight=77.0; g=9.80665
mu=unit_weight/g*A; mt=mu*L
M=(mt/6)*np.array([[2,1],[1,2]],dtype=float)
K=(E*A/L)*np.array([[1,-1],[-1,1]],dtype=float)
r=np.ones(2)
b=M@r
activeMass=M[1,1]
lam,vec=eigh(K[1:,1:],M[1:,1:]);omega=math.sqrt(lam[0]);phi=np.array([0,vec[0,0]])
Gamma=float(phi@b)
assert abs(Gamma*Gamma/mt-.75)<1e-12
assert abs(float(r@M@r)-mt)<1e-12
sa=2.5; Sd=sa/omega**2
predicted=abs(phi[1]*Gamma*Sd)
expected=1.5*activeMass*sa/(E*A/L)
assert abs(predicted-expected)<1e-15
ag=1.7
relativeTipAcceleration=-float(b[1]/activeMass)*ag
absoluteTipAcceleration=relativeTipAcceleration+ag
assert abs(relativeTipAcceleration+1.5*ag)<1e-12
assert abs(absoluteTipAcceleration+.5*ag)<1e-12
# Simple Newmark test in the same single physical DOF, constant base acceleration.
dt=0.001; damp=.02
v0=0.;u0=0.;a0=relativeTipAcceleration
for j in range(12):
    stiff=omega**2+2*omega*damp*2/dt+4/dt**2
    u_pred=u0+dt*v0+(.25*dt**2)*a0
    v_pred=v0+.5*dt*a0
    u=(relativeTipAcceleration+4*u_pred/dt**2+2*omega*damp*(2*u_pred/dt-v_pred))/stiff
    # Equivalent to Newmark effective force, not using any project C# code.
    acc=4*(u-u_pred)/dt**2
    vel=v_pred+.5*dt*acc
    assert all(math.isfinite(x) for x in (u,vel,acc))
    u0,v0,a0=u,vel,acc
print('M60 independent: ground mass =',mt,'eff mass fraction=',Gamma**2/mt)
print('M60 independent: frequency Hz =',omega/(2*math.pi),'SRSS one-mode tip =',predicted)
print('M60 independent: t=0 tip relative/absolute acceleration =',relativeTipAcceleration,absoluteTipAcceleration)
print('M60 independent 1DOF Newmark finite-time displacement=',u0)
