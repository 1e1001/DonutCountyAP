using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Randomizer;

public class ExtraDeliveryInfo
{
    // 1:1 mapping with delivery indexes
    static ExtraDeliveryInfo[] INFO = [
        // TODO: don't send segment checks if the setting is off!
        // how to do this in a better way??
        new ExtraDeliveryInfo(false, CheckId.DeliverMirasHouse, [CheckId.SegmentMirasHouse1]),
        new ExtraDeliveryInfo(true, CheckId.None, []), // bk texting cutscene
        new ExtraDeliveryInfo(false, CheckId.DeliverPottersRock, [CheckId.SegmentPottersRock1, CheckId.SegmentPottersRock2, CheckId.SegmentPottersRock3, CheckId.SegmentPottersRock4, CheckId.SegmentPottersRock5]),
        new ExtraDeliveryInfo(false, CheckId.DeliverRangerStation, [CheckId.SegmentRangerStation1, CheckId.SegmentRangerStation2, CheckId.SegmentRangerStation3]),
        new ExtraDeliveryInfo(false, CheckId.DeliverRiverbed, [CheckId.SegmentRiverbed1]),
        new ExtraDeliveryInfo(false, CheckId.DeliverCampground, [CheckId.SegmentCampground1, CheckId.SegmentCampground2]),
        new ExtraDeliveryInfo(false, CheckId.DeliverHopperSprings, [CheckId.SegmentHopperSprings1, CheckId.SegmentHopperSprings2, CheckId.SegmentHopperSprings3, CheckId.SegmentHopperSprings4]),
        new ExtraDeliveryInfo(false, CheckId.DeliverJoshuaTree, [CheckId.SegmentJoshuaTree1]),
        new ExtraDeliveryInfo(false, CheckId.DeliverBeachLotC, [CheckId.SegmentBeachLotC1, CheckId.SegmentBeachLotC2, CheckId.SegmentBeachLotC3]),
        new ExtraDeliveryInfo(false, CheckId.DeliverGeckoPark, [CheckId.SegmentGeckoPark1, CheckId.SegmentGeckoPark2]),
        // SegmentChickenBarn2 is store exit
        new ExtraDeliveryInfo(false, CheckId.DeliverChickenBarn, [CheckId.SegmentChickenBarn1, CheckId.SegmentChickenBarn3, CheckId.SegmentChickenBarn4, CheckId.SegmentChickenBarn5]),
        new ExtraDeliveryInfo(true, CheckId.DeliverHoneyNutForest, [CheckId.SegmentHoneyNutForest1, CheckId.SegmentHoneyNutForest2]),
        new ExtraDeliveryInfo(false, CheckId.DeliverCatSoup, [CheckId.SegmentCatSoup1, CheckId.SegmentCatSoup2, CheckId.SegmentCatSoup3, CheckId.SegmentCatSoup4]),
        new ExtraDeliveryInfo(true, CheckId.DeliverDonutShop, [CheckId.SegmentDonutShop1]),
        new ExtraDeliveryInfo(true, CheckId.DeliverAbandonedHouse, [CheckId.SegmentAbandonedHouse1, CheckId.SegmentAbandonedHouse2]),
        new ExtraDeliveryInfo(true, CheckId.DeliverRaccoonLagoon, [CheckId.SegmentRaccoonLagoon1, CheckId.SegmentRaccoonLagoon2, CheckId.SegmentRaccoonLagoon3, CheckId.SegmentRaccoonLagoon4]),
        // SegmentThe4053 is QuadcopterBigBoy
        new ExtraDeliveryInfo(true, CheckId.DeliverThe405, [CheckId.SegmentThe4051, CheckId.SegmentThe4052, CheckId.SegmentThe4054]),
        new ExtraDeliveryInfo(true, CheckId.None, []), // above donut county
        new ExtraDeliveryInfo(false, CheckId.SegmentRaccoonHQ3, [CheckId.SegmentRaccoonHQ1, CheckId.None, CheckId.SegmentRaccoonHQ2]),
        new ExtraDeliveryInfo(true, CheckId.DeliverRaccoonHQ, []), // hq entrance interior
        new ExtraDeliveryInfo(false, CheckId.SegmentBiologyLab3, [CheckId.SegmentBiologyLab1, CheckId.SegmentBiologyLab2]),
        new ExtraDeliveryInfo(true, CheckId.DeliverBiologyLab, []), // path to anthropology
        // TODO: SegmentAnthroplogyLab2 doesn't trigger
        new ExtraDeliveryInfo(false, CheckId.SegmentAnthroplogyLab3, [CheckId.SegmentAnthroplogyLab1, CheckId.SegmentAnthroplogyLab2]),
        new ExtraDeliveryInfo(true, CheckId.DeliverAnthropologyLab, []), // path to tk office
        // checks in TKOfficeManager
        new ExtraDeliveryInfo(true, CheckId.None, []),
        // DeliveryBossFight is tornado
        // TODO: SegmentBossFight1 (on victory)
        new ExtraDeliveryInfo(true, CheckId.None, [CheckId.SegmentBossFight2]), // bossfight
        new ExtraDeliveryInfo(true, CheckId.None, []), // catapult
        new ExtraDeliveryInfo(true, CheckId.None, []), // aftermath
        new ExtraDeliveryInfo(false, CheckId.None, []), // game over
    ];
    public static ExtraDeliveryInfo GetCurrent()
    {
        return Plugin.GameState.ActiveDelivery ? INFO[DataManager.GetCurrentDelivery()] : null;
    }

    // false for levels that transition to results (results is their end)
    public bool EndOfLevel;
    public CheckId FinishLocation;
    public CheckId[] StartCameraLocations;

    ExtraDeliveryInfo(bool _endOfLevel, CheckId _finishLocation, CheckId[] _startCameraLocations) {
        EndOfLevel = _endOfLevel;
        FinishLocation = _finishLocation;
        StartCameraLocations = _startCameraLocations;
    }
    // TODO: trashsanity details would be indexed per-scene instead of here (including scouted indicators?)
}

