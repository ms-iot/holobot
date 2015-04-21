AntennaHeadDiameter = 9;
AntennaHeadWall = 3;
AntennaHeadLength = 11;
AntennaDiameter = 2.5;
AntennaMountLength = 15;
MountLength = 20;
Slot = AntennaDiameter;

LifeCamWall = 2.5;
LifeCamHousingDiameter = 30;
LifeCamDiameter = LifeCamHousingDiameter + 2 * LifeCamWall;
LifeCamLength = 30;
LifeCamSpeakerOffset = 15;
LifeCamSpeakerWidth = 15;
LifeCamMountOffset = 15;
LifeCamMountWidth = 20;
LifeCamMountOffset = 20;
LifeCamPivotWidth = 20;

M4ScrewDiameter = 4.3;

CounterWeightDiameter = 40;
CounterWeightWidth = 7;
CounterWeightLength = CounterWeightDiameter/2 + 5;
CounterWeightOffset = 30;

MountWing =  AntennaHeadDiameter;

kFudge = .01;
$fn=50;
module LifeCamHousing()
{
    difference()
    {
        union()
        {
            cylinder(h=LifeCamLength, d=LifeCamDiameter);
        
            translate([-LifeCamPivotWidth/2,AntennaMountLength/2-LifeCamDiameter+1, LifeCamLength-LifeCamSpeakerWidth-kFudge])
                cube([LifeCamPivotWidth, 
                        AntennaMountLength/2+5, 
                        AntennaMountLength]);

            translate([-LifeCamPivotWidth/2,AntennaMountLength/2-LifeCamDiameter+1, LifeCamLength-LifeCamSpeakerWidth-kFudge])
                cube([LifeCamPivotWidth, 
                        AntennaMountLength/2+5, 
                        AntennaMountLength]);

            rotate([90, 0, 90])
                translate([-LifeCamDiameter+1 + AntennaMountLength/2, AntennaMountLength/2 + LifeCamLength-LifeCamSpeakerWidth-kFudge, -LifeCamPivotWidth/2])
                cylinder(h=LifeCamPivotWidth, d=AntennaMountLength);
        }
        translate([-LifeCamSpeakerOffset/2,LifeCamHousingDiameter/2 - LifeCamSpeakerWidth/2,-kFudge])
            cube([LifeCamSpeakerWidth, 
                     LifeCamSpeakerWidth, 
                        LifeCamLength - LifeCamSpeakerOffset]);
        
        translate([-LifeCamMountOffset/2,-LifeCamHousingDiameter,-kFudge])
            cube([LifeCamMountWidth, 
                     LifeCamMountWidth, 
                        LifeCamLength - LifeCamMountOffset]);
		translate([0, 0, -kFudge])
		cylinder(h=LifeCamLength + 2 * kFudge, 
                d=LifeCamDiameter - 2 * LifeCamWall);
		
        translate([-AntennaHeadDiameter/2, -LifeCamDiameter,LifeCamLength -2*AntennaHeadDiameter -2*kFudge])
                cube([AntennaHeadDiameter, 
                      3*AntennaHeadDiameter, 
                      MountLength + 2 * kFudge]);
        
            rotate([90, 0, 90])
                translate([-LifeCamDiameter+1 + AntennaMountLength/2, AntennaMountLength/2 + LifeCamLength-LifeCamSpeakerWidth-kFudge, -LifeCamPivotWidth/2])
            cylinder(h=2*AntennaMountLength, d=M4ScrewDiameter);
    }
}

module AntennaMountClip()
{
    difference()
    {
        union()
        {
            translate([AntennaHeadDiameter/2, -AntennaHeadDiameter/2,-kFudge])
            cube([AntennaMountLength/2 + 2, 
                AntennaHeadDiameter, 
                AntennaMountLength]);
            rotate([90, 0, 0])
            translate([AntennaHeadDiameter + AntennaHeadDiameter/2, AntennaMountLength/2, -AntennaHeadDiameter/2])
            cylinder(h=AntennaHeadDiameter, d=AntennaMountLength);
            
            cylinder(h=AntennaMountLength, d=AntennaHeadDiameter + 2 * AntennaHeadWall);


			translate([-3,0,-2.7])
			rotate([0, 35, 0])
			{
			translate([-CounterWeightLength - CounterWeightOffset, CounterWeightWidth/2, CounterWeightDiameter/2])
			rotate([90,0, 0])
            cylinder(h=CounterWeightWidth, d=CounterWeightDiameter);
			
			translate([-CounterWeightOffset - AntennaHeadDiameter - CounterWeightDiameter/2 + 3, -CounterWeightWidth/2, 0])
            cube([CounterWeightOffset + AntennaHeadDiameter/2 + CounterWeightDiameter/2 - 3, 
                CounterWeightWidth, 
                AntennaMountLength]);
			}
        }
        
    
        union()
        {
            translate([0, 0, -kFudge])
            cylinder(h=AntennaMountLength+2*kFudge, d=AntennaDiameter);
            translate([0, 0, -kFudge])
            cylinder(h=AntennaHeadLength+2*kFudge, d=AntennaHeadDiameter);

            translate([0, -Slot/2,-2*kFudge])
                cube([3*AntennaHeadDiameter, 
                      Slot, 
                      MountLength + 2 * kFudge]);
            
            rotate([90, 0, 0])
            translate([AntennaHeadDiameter + AntennaHeadDiameter/2, AntennaMountLength/2, -AntennaMountLength/2])
            cylinder(h=AntennaMountLength, d=M4ScrewDiameter);
			
			translate([-3,0,-2.7])
			rotate([0, 35, 0])
			translate([-CounterWeightLength - CounterWeightOffset, CounterWeightWidth/2 + kFudge, CounterWeightDiameter/2])
			rotate([90,0, 0])
            cylinder(h=CounterWeightWidth + 2 * kFudge, d=M4ScrewDiameter);
			
			
            
        }
    }
}

AntennaMountClip();
//LifeCamHousing();