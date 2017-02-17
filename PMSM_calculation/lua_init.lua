cos=math.cos
sin=math.sin
pi=math.pi
deg=math.deg
rad=math.rad
step=0
time=0
omegarad=0


-- Squarewave T-cycle time, w-width of square, 100% is T/2
function squarewave(T,w,max,min,t)
	if (t>T or t<0) then
		t = t-math.floor(t/T)*T
	end	

	local t1 = T/4*(1-w)
	local t2 = t1+w*T/2
	local t3 = T/2+t1
	local t4 = t3+w*T/2

	if t>=t1 and t<=t2 then
		return max
	elseif t>=t3 and t<=t4 then 
		return min
	else 
		return 0
	end
end