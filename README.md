# DonutCountyAP
Archipelago implementation for Donut County (2018)

[Game info](./donutcounty/docs/en_Donut County.md) / [Setup guide](./donutcounty/docs/en_setup.md) / [Latest release](https://github.com/1e1001/DonutCountyAP/releases/latest)

## Developer guide

- `logic.xml` is a spreadsheet containing all the item/location info
	- if you change it, run `autologic.fsx` and commit its changes
- To get working mod builds:
	- add a modded copy of the game as `lib/Donut County/`
	- add a compiled copy of [`c-wspp-websocket-sharp`](https://github.com/black-sliver/c-wspp-websocket-sharp/) to `lib/wspp/`
- To develop APWorld, hardlink (junction) the `donutcounty/` folder into your from-source Archipelago's `worlds/` folder

## design notes / todo:

- fuzzing only has a 70% success rate but i'm fairly sure that's just from restrictive yaml settings
	- fix my fuzzing so i can actually use the empty world
- snake danger has no way to collect locations after items (but it's kinda fun)
- clamp mouse position to screen coordinates (there's some weird menu behaviors)
- make the cursor visible in some situations it normally isn't
- less imgui
	- replace archipelago console with a notifications feed
- disable steam autorestart? or do i just add the appid hack to the guide
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
	- allow clicking on level circles
- level select tracker (real)
	- grid of dots to the side of the preview image
	- show location types / names somehow?
- items tracker
	- in main menu?
	- or in the pause stats again

testing todo:
- gog version
	- datamanager might need different hooks entirely?
- proton
- macos? terrifying concept