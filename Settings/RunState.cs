using System.Collections.Generic;

namespace StormShooter;

public class RunState
{
    public int Stage = 1;
    public float Health;
    public Gun MainGun = GunData.ScrapRifle;
    public Gun SidearmGun = GunData.Pistol;
    public int ActiveSlot = 1;
    public int LightAmmo = 20;
    public int MediumAmmo = 15;
    public int HeavyAmmo = 5;
    public Dictionary<Gun, int> MagAmmo = new();

    public static RunState CreateNew()
    {
        return new RunState
        {
            Health = 990f,
            MagAmmo = new Dictionary<Gun, int>
            {
                [GunData.ScrapRifle] = (int)GunData.ScrapRifle.MagSize,
                [GunData.Pistol] = (int)GunData.Pistol.MagSize,
            }
        };
    }

    public RunState AdvanceStage(float health, Gun mainGun, Gun sidearmGun, int activeSlot, GunController guns)
    {
        var next = new RunState
        {
            Stage = Stage + 1,
            Health = health,
            MainGun = mainGun,
            SidearmGun = sidearmGun,
            ActiveSlot = activeSlot,
            LightAmmo = guns.GetPoolAmmo(AmmoType.Light),
            MediumAmmo = guns.GetPoolAmmo(AmmoType.Medium),
            HeavyAmmo = guns.GetPoolAmmo(AmmoType.Heavy),
        };
        next.MagAmmo[mainGun] = guns.GetCurrentAmmo(mainGun);
        next.MagAmmo[sidearmGun] = guns.GetCurrentAmmo(sidearmGun);
        return next;
    }
}
