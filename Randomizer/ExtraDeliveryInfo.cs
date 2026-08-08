using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DonutCountyAP.Randomizer;

public class ExtraDeliveryInfo
{
    // TODO: kill this
    static ExtraDeliveryInfo[] INFO = [
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(true), // bk texting cutscene
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(true),
        new ExtraDeliveryInfo(true),
        new ExtraDeliveryInfo(true),
        new ExtraDeliveryInfo(true),
        new ExtraDeliveryInfo(true), // above donut county
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(true), // hq entrance interior
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(true), // path to anthropology
        new ExtraDeliveryInfo(false),
        new ExtraDeliveryInfo(true), // path to tk office
        new ExtraDeliveryInfo(true),
        new ExtraDeliveryInfo(true), // bossfight
        new ExtraDeliveryInfo(true), // catapult
        new ExtraDeliveryInfo(true), // aftermath
        new ExtraDeliveryInfo(false), // game over
    ];
    public static ExtraDeliveryInfo GetCurrent(out int index)
    {
        index = DataManager.GetCurrentDelivery();
        return Plugin.GameState.ActiveDelivery ? INFO[index] : null;
    }

    // false for levels that transition to results (results is their end)
    public bool EndOfLevel;

    ExtraDeliveryInfo(bool _endOfLevel) {
        EndOfLevel = _endOfLevel;
    }
}

