from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld

from .options import option_groups, option_presets

class DonutCountyWebWorld(WebWorld):
    game = "Donut County"
    theme = "grassFlowers"
    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "A guide to setting up the DonutCountyAP mod.",
        "English",
        "setup_en.md",
        "setup/en",
        ["1e1001"],
    )
    tutorials = [setup_en]
    option_groups = option_groups
    options_presets = option_presets