# DonutCountyAP
Archipelago implementation for Donut County (2018)

[Game info](./donutcounty/docs/en_Donut%20County.md) / [Setup guide](./donutcounty/docs/en_setup.md) / [Latest release](https://github.com/1e1001/DonutCountyAP/releases/latest)

## AI non-usage disclosure

Generative AI has not been used to make this AP implementation. I have no plans to start using generative AI.

## Developer guide

- `logic.xml` is a spreadsheet containing all the item/location info
	- if you change it, run `autologic.fsx` and commit its changes
- To get working mod builds:
	- add a modded copy of the game as `lib/Donut County/`
	- add a compiled copy of [`c-wspp-websocket-sharp`](https://github.com/black-sliver/c-wspp-websocket-sharp/) to `lib/wspp/`
- To develop APWorld, hardlink (junction) the `donutcounty/` folder into your from-source Archipelago's `worlds/` folder
	- the `.pyproj` has a hardcoded-for-me search path for IDE assistance, not sure how to make that more generalizable yet.

## design notes / todo:

- different piece-percentage for level unlocks vs. goal unlock
	- or like one piece = one level
	- do actual playtesting / design work for this
		- in sync situations it seems any kind of level locking is bad?
		- and it seems in fuzzing that the piece requirements often outpace the available checks in each level
- "Quadcopter Piece" is a temporary name, come up with a better one and use that
	- name should try to reflect the usefulness of the item?
- run with logic test world
- if levels = false, then levels in start_inventory should set their piece requirements to 0
- fuzzing has >1% failure rate due to restrictive start / piece progression
	- fix my fuzzing setup so i can actually use the empty world
- fix weird sudden crashes
- snake danger doesn't have a way to delay the 4-item transition to collect the locations, but it's still technically doable :)
- clamp mouse position to screen coordinates (there's some weird menu behaviors)
- make the cursor visible in some situations it normally isn't (e.g. during loading)
- less imgui
	- replace archipelago console with a notifications feed
- disable steam autorestart? or do i just add the appid hack to the install guide
- trashsanity
	- trashopedia shows collection status next to item
	- optional in-level indicator (some billboard above item?)
- easier achievements: make mira's bossfight hp linear
- music shuffle
- texting qol
	- allow pressing chat buttons with space (this is much harder than i anticipated! regular button raycast doesn't hit ui elements)
	- look into how controller-as-mouse is implemented?
- level select ui
	- ui for locked levels
	- allow clicking on level circles for fast navigation
- level select tracker (real)
	- grid of dots to the side of the preview image
	- show location types / names somehow?
- items tracker
	- in main menu?
	- or in the pause stats again
- restart button for more levels
- poll timers for missed abilities
	- within 1s after failing a hole ability, if you have the item it'll run the effect properly
- some kinda precommit hook to remove persisted view state from logic.xml
- address/password input should strip some spaces / search in-text for their value
	- see how text client does this, or that other impl i forgot about

testing todo:
- gog version
	- datamanager might need different hooks entirely?
- proton
- macos? terrifying