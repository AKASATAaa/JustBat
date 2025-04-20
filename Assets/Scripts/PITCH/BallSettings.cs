using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class BallSettings
{
    public int name;
    public float throwPower;
    public float vtorqueSpeed;
    public float ztorqueSpeed;
    public float voffset;
    public float hoffset;
    public float ballAngleX;
    public float ballAngleZ;
    public float brakingStartTime;
    public float maxWobble;

    public BallSettings(int name, float throwPower, float vtorqueSpeed, float ztorqueSpeed, float voffset, float hoffset, float ballAngleX, float ballAngleZ, float brakingStartTime, float maxWobble)
    {
        this.name = name;
        this.throwPower = throwPower;
        this.vtorqueSpeed = vtorqueSpeed;
        this.ztorqueSpeed = ztorqueSpeed;
        this.voffset = voffset;
        this.hoffset = hoffset;
        this.ballAngleX = ballAngleX;
        this.ballAngleZ = ballAngleZ;
        this.brakingStartTime = brakingStartTime;
        this.maxWobble = maxWobble;
    }
}