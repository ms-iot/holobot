$fn=20;
MicroSwitchWidth = 5.7;// mm
MicroSwitchHeight = 6.5; // mm
MicroSwitchLength = 12.9; // mm
MicroSwitchArmLength = 13; // mm
MicroSwitchArmThickness = .1; // mm
MicroSwitchMountHolesDiameter = 2.2; // mm
MicroSwitchMountHolesOffset = 3.1; // mm

StopBlockOpening = 32; //mm		// Opening is 32mm
StopBlockWidth = StopBlockOpening / 2 - MicroSwitchWidth;
StopBlockDepth = 7; // mm
StopBlockHeight = 14; //mm


module NeckStopMount()
{
	translate([-StopBlockWidth - MicroSwitchWidth/2, -StopBlockDepth/2, 0])
		cube([StopBlockWidth, StopBlockDepth, StopBlockHeight]);
}

module Microswitch()
{
	difference()
	{
		translate([0, 0, -1.5])
		{
		rotate([15, 0, 0])
			translate([-MicroSwitchWidth/2, -MicroSwitchLength/2 + 4, MicroSwitchHeight + 1])
				cube([MicroSwitchWidth, MicroSwitchArmLength, MicroSwitchArmThickness]);
		
		translate([-MicroSwitchWidth/2, -MicroSwitchLength/2, 0])
			cube([MicroSwitchWidth, MicroSwitchLength, MicroSwitchHeight]);
		}
	
		translate([-MicroSwitchWidth,MicroSwitchMountHolesOffset,0])
			rotate([90,0,90])
				cylinder(h=MicroSwitchWidth*2, d=MicroSwitchMountHolesDiameter);

		translate([-MicroSwitchWidth,-MicroSwitchMountHolesOffset,0])
			rotate([90,0,90])
				cylinder(h=MicroSwitchWidth*2, d=MicroSwitchMountHolesDiameter);
	}
}

/*
translate([0, 0, StopBlockHeight/2])
rotate([60, 0, 0])
Microswitch();
*/
NeckStopMount();
