using Autodesk.Revit.DB;
using System;

namespace RevitMCP;

/// <summary>
/// Unit conversion utilities enforcing RevitMCP Rule 3:
/// - Lengths in mm
/// - Angles in degrees
/// - Areas in m²
/// - Volumes in m³
/// </summary>
public static class UnitConversions
{
    public const double MmPerFoot = 304.8;
    public const double FeetPerMm = 1.0 / MmPerFoot;
    public const double SqMPerSqFt = 0.09290304;
    public const double SqFtPerSqM = 1.0 / SqMPerSqFt;
    public const double CuMPerCuFt = 0.028316846592;
    public const double CuFtPerCuM = 1.0 / CuMPerCuFt;
    public const double DegToRadFactor = Math.PI / 180.0;
    public const double RadToDegFactor = 180.0 / Math.PI;

    public static double MmToFeet(double mm) => mm * FeetPerMm;
    public static double FeetToMm(double feet) => feet * MmPerFoot;

    public static XYZ MmToFeet(XYZ ptMm) => new(ptMm.X * FeetPerMm, ptMm.Y * FeetPerMm, ptMm.Z * FeetPerMm);
    public static XYZ FeetToMm(XYZ ptFeet) => new(ptFeet.X * MmPerFoot, ptFeet.Y * MmPerFoot, ptFeet.Z * MmPerFoot);

    public static double DegToRad(double deg) => deg * DegToRadFactor;
    public static double RadToDeg(double rad) => rad * RadToDegFactor;

    public static double SqFtToM2(double sqFt) => sqFt * SqMPerSqFt;
    public static double M2ToSqFt(double m2) => m2 * SqFtPerSqM;

    public static double CuFtToM3(double cuFt) => cuFt * CuMPerCuFt;
    public static double M3ToCuFt(double m3) => m3 * CuFtPerCuM;
}
