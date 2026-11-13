namespace JabaSurvivol.ArtGenerator;

internal enum CharacterPart
{
    Shadow,
    Leg,
    Torso,
    Head,
    Arm
}

internal sealed record CharacterShape(
    float TorsoWidth,
    float TorsoHeight,
    float TorsoOffsetY,
    float HeadRadius,
    float HeadOffsetX,
    float HeadOffsetY,
    float ArmLength,
    float ArmThickness,
    float LegWidth,
    float LegLength,
    float ShoulderWidth,
    bool Ragged,
    bool Horns,
    bool Core);
