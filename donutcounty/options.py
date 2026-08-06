from dataclasses import dataclass

from Options import Choice, OptionGroup, PerGameCommonOptions, Range, Toggle, OptionCounter

class GoalArea(Choice):
    """
	- bossfight: Beat the bossfight level to unlock Aftermath
	- aftermath: Fragments directly unlock Aftermath
    """
    display_name = "Goal area"
    option_bossfight = 0
    option_aftermath = 1
    default = option_bossfight
   
class TotalFragments(Range):
    """
    How many fragments to add to the item pool. There may be fewer than this depending on the number of free locations
    """
    display_name = "Total fragments"
    range_start = 0
    range_end = 100
    default = 50
    
class FragmentsRequired(Range):
    """
    Percentage of total fragments that need to be collected to unlock the goal area
    """
    display_name = "% fragments required"
    range_start = 0
    range_end = 100
    default = 80
    
class FragmentsUnlockLevels(Choice):
    """
    - off: All levels are available
    - sequence: Start with only Mira's House, fragments progressively unlock following areas
    - random: Starting area and order of unlocked areas is randomized
    """
    display_name = "Fragments unlock levels"
    option_off = 0
    option_sequence = 1
    option_random = 2
   
class Levels(Toggle):
    """
    Require items to be able to access each level. Place Level Unlock in your start_inventory so you don't have an empty first sphere. (21 items)
    """
    display_name = "Levels"
   
class HoleWater(Toggle):
    """
    Require an item to be able to fill your hole with water or soup (1 item)
    """
    display_name = "Hole water"
    
class HoleFire(Toggle):
    """
    Require an item to be able to fill your hole with fire, or to have fire ignite things in the world (1 item)
    """
    display_name = "Hole fire"
    
class HoleSnake(Toggle):
    """
    Require an item to be able to fill your hole with a snake (1 item)
    """
    display_name = "Hole snake"
    
class HoleLight(Toggle):
    """
    Require an item to be able to fill your hole with light (1 item)
    """
    display_name = "Hole light"
    
class HoleBunnies(Toggle):
    """
    Require an item to be able to have bunnies mate in your hole (1 item)
    """
    display_name = "Hole bunnies"
    
class Catapult(Choice):
    """
	- off: Catapult is always available
	- global: Require an item to use the catapult (1 item)
	- indiviudal: Each type of launchable object gets its own item (10 items)
    """
    display_name = "Catapult"
    option_off = 0
    option_global = 1
    option_individual = 2
    default = option_global
    
class LevelCompletions(Toggle):
    """
    Location for completing each level (21 locations)
    """
    display_name = "Level completions"
    
class LevelSegments(Toggle):
    """
    Location for completing segments of levels (58 locations)
    """
    display_name = "Level segments"
    
class Achievements(Toggle):
    """
    Location for each non-postgame achievement (16 locations)
    """
    display_name = "Achievements"
    
class BuyCatapult(Toggle):
    """
    Location when purchasing the catapult (1 location)
    """
    display_name = "Buy catapult"
    
class SnakeDanger(Toggle):
    """
    Ranger Station snake danger is randomized (4 locations, 4 items)
    """
    display_name = "Snake danger"
    
class SaltAndPepper(Toggle):
    """
    Cat Soup shakers will give locations, progressive salt & paper unlock progression (5 locations, 5 items)
    """
    display_name = "Salt & pepper"
    
class HackProtocol(Toggle):
    """
    Raccoon HQ's entrance is randomized (1 location, 1 check)
    """
    display_name = "Hack protocol"

_default_filler_weights = {
    "filler": 7,
    "concrete_trap": 2,
    "depths_trap": 1,
}

class FillerWeights(OptionCounter):
    """
    If there's too many locations, add some filler!
    - filler: Does (basically) nothing
    - concrete_trap: Disables your hole for a short period
    - depths_trap: Sends you to a random cutscene 999ft below Donut County
    """
    display_name = "Filler Weights"
    valid_keys = _default_filler_weights.keys()

    min = 0

    default = _default_filler_weights

@dataclass
class DonutCountyOptions(PerGameCommonOptions):
    # Game options
    goal_area: GoalArea
    total_fragments: TotalFragments
    fragments_required_percent: FragmentsRequired
    fragments_unlock_levels: FragmentsUnlockLevels

    # Item options
    levels: Levels
    hole_water: HoleWater
    hole_fire: HoleFire
    hole_snake: HoleSnake
    hole_light: HoleLight
    hole_bunnies: HoleBunnies
    catapult: Catapult

    # Location options
    # for now, level_completions will be forced true
    #level_completions: LevelCompletions
    level_segments: LevelSegments
    achievements: Achievements
    buy_catapult: BuyCatapult
    snake_danger: SnakeDanger
    salt_and_pepper: SaltAndPepper
    hack_protocol: HackProtocol
    
    # Trash options
    filler_weights: FillerWeights

option_groups = [
    OptionGroup("Game Options", [
        GoalArea, TotalFragments, FragmentsRequired
    ]),
    OptionGroup("Item Options", [
        HoleWater, HoleFire, HoleSnake, HoleLight, HoleBunnies, Catapult
    ]),
    OptionGroup("Location Options", [
        LevelCompletions, LevelSegments, Achievements, BuyCatapult, SnakeDanger, SaltAndPepper, HackProtocol
    ]),
    OptionGroup("Trash Options", [
        FillerWeights
    ]),
]

option_presets = {
    "default": {
        "goal_area": "bossfight",
        "total_fragments": 50,
        "fragments_required_percent": 80,
        "water": True,
        "fire": True,
        "snake": True,
        "light": True,
        "catapult": "global",
        "level_segments": True,
        "achievements": True,
        "buy_catapult": True,
        "snake_danger": True,
        "salt_and_pepper": True,
        "hack_protocol": True,
        "filler_weights": _default_filler_weights,
    }
}