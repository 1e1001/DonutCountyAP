from dataclasses import dataclass

from Options import Choice, OptionGroup, PerGameCommonOptions, Range, Toggle, OptionCounter, StartInventoryPool

class GoalArea(Choice):
    """
    The game will goal upon entering Aftermath.
    - bossfight: Unlock and complete the Boss Fight level to unlock Aftermath
    - aftermath: Quadcopter Pieces directly unlock Aftermath
    """
    display_name = "Goal area"
    option_bossfight = 0
    option_aftermath = 1
    default = option_bossfight
   
class TotalPieces(Range):
    """
    How many Quadcopter Pieces to add to the item pool.
    There may be fewer than this depending on the number of free locations.
    """
    display_name = "Total pieces"
    range_start = 0
    range_end = 100
    default = 40
    
class PiecesRequired(Range):
    """
    Percentage of total Quadcopter Pieces that need to be collected to unlock the goal area.
    """
    display_name = "% pieces required"
    range_start = 0
    range_end = 100
    default = 80
    
class Levels(Toggle):
    """
    Require items to be able to access each level. Automatically gives you one level to start (19-20 items)
    """
    display_name = "Levels"
   
class Hole(Choice):
    """
    - off: Hole contents always have effects.
    - global: Require an item to have effects from hole contents. (1 item)
    - split: Require items to have effects from each individual type of hole contents. (5 items)
    """
    display_name = "Hole effects"
    option_off = 0
    option_global = 1
    option_split = 2
    default = option_split
    
class Catapult(Choice):
    """
    - off: Catapult is always available.
    - global: Require an item to use the catapult. (1 item, 1 location)
    - split: Require items to launch specific kinds of object. (11 items, 1 location)
    """
    display_name = "Catapult"
    option_off = 0
    option_global = 1
    option_split = 2
    default = option_global
    
class Texting(Toggle):
    """
    Require an item to be able to send text messages. (1 item)
    """
    display_name = "Texting"
    
class TrashSouls(Toggle):
    """
    Require an item to unlock physics on each type of trash. Has no effect if `trashsanity` is `off`. (113 items)
    """
    display_name = "Texting"
    
#class LevelCompletions(Toggle):
#    """
#    Location for completing each level. (21 locations)
#    """
#    display_name = "Level completions"
#    
#class LevelSegments(Toggle):
#    """
#    Location for completing segments of levels. (58 locations)
#    """
#    display_name = "Level segments"
    
class Achievements(Toggle):
    """
    Location for each non-postgame achievement. (16 locations)
    """
    display_name = "Achievements"
  
    
class SnakeDanger(Toggle):
    """
    Ranger Station snake danger is randomized. (4 locations, 4 items)
    """
    display_name = "Snake danger"
    
class SaltAndPepper(Toggle):
    """
    Cat Soup shakers will give locations, Progressive Salt & Pepper unlock progression. (5 locations, 5 items)
    """
    display_name = "Salt & pepper"
    
class Trashsanity(Choice):
    """
    - off: Trash gives no checks
    - types: Collecting each type of trash is a location (113 locations)
    - all: Collecting each individual piece of trash is a location (1276 locations)
    """
    display_name = "Trashsanity"
    option_off = 0
    option_types = 1
    option_all = 2
    default = option_off

_default_filler_weights = {
    "filler": 12,
    # unimplemented
    #"hole_size": 0,
    #"launch_trap": 0,
    "cement_trap": 2,
    "depths_trap": 1,
}

class FillerWeights(OptionCounter):
    """
    If there's too many locations, add some filler!
    - filler: Does (basically) nothing.
    - concrete_trap: Disables your hole for a short period.
    - depths_trap: Takes you 999ft below Donut County for a random cutscene.
    """
    #- hole_size: Gives your hole a temporary size boost.
    #- launch_trap: Launch a random item from your hole.
    display_name = "Filler Weights"
    valid_keys = _default_filler_weights.keys()

    min = 0

    default = _default_filler_weights

# item balancing notes:
# min. unpaired locations:
# - levels: 21
# - segments: 58
# - total: 79
# max. unpaired items:
# - levels: 21 (goal = aftermath)
# - holes: 5
# - texting: 1
# - catapults: 11
# - total: 38
# space for pieces: 41, so default = 40

@dataclass
class DonutCountyOptions(PerGameCommonOptions):
    # Game options
    goal_area: GoalArea
    total_pieces: TotalPieces
    pieces_required_percent: PiecesRequired

    # Item options
    levels: Levels
    hole: Hole
    catapult: Catapult
    texting: Texting
    trash_souls: TrashSouls

    # Location options
    #level_completions: LevelCompletions
    #level_segments: LevelSegments
    achievements: Achievements
    snake_danger: SnakeDanger
    salt_and_pepper: SaltAndPepper
    trashsanity: Trashsanity
    
    # Filler options
    filler_weights: FillerWeights

    # Default AP option (why)
    start_inventory_from_pool: StartInventoryPool

option_groups = [
    OptionGroup("Game Options", [
        GoalArea, TotalPieces, PiecesRequired
    ]),
    OptionGroup("Item Options", [
        Levels, Hole, Catapult, Texting, TrashSouls
    ]),
    OptionGroup("Location Options", [
        Achievements, SnakeDanger, SaltAndPepper, Trashsanity
    ]),
    OptionGroup("Filler Options", [
        FillerWeights
    ]),
]

option_presets = {
    "default": {
        # TODO: adjust this to a sync-friendly preset
        "goal_area": "bossfight",
        "total_pieces": 40,
        "pieces_required_percent": 80,
        "pieces_unlock_levels": False,
        "levels": False,
        "hole": "split",
        "catapult": "split",
        "texting": True,
        "achievements": True,
        "buy_catapult": True,
        "snake_danger": True,
        "salt_and_pepper": True,
        "filler_weights": _default_filler_weights,
    }
}