# Wizard’s Wish
 
I decided to use CS50Gs final project as an opportunity to learn Unity’s fundamentals and practice C#. I love real time strategy games, so I decided to make one and really focus on building out some core systems that are common to the genre. Admittedly, I got a little carried away and this took much longer than I initially planned.
 
Wizard’s wish is an RTS (real time strategy) game with the objective of surviving several waves of zombies that are spawned and will try to attack your base. The map is also seeded with 1000 idle wandering zombies that will attack you when they see you. This adds some challenge to the play between zombies waves as you try to explore the map and find more resources. For this concept I drew inspiration from existing RTS games such as [They Are Billions](https://store.steampowered.com/app/644930/They_Are_Billions/).
 
The name Wizard’s Wish was chosen early in development. Initially I wanted the game to be themed on a Wizard in his tower who is trying to buy enough time to cast a powerful spell whilst hordes of enemies besiege him. I opted for a simpler execution of this theme, and didn’t end up fully implementing stylistic elements that support it. The exception being that the player's central structure is a Wizard’s tower that can cast powerful spells.
 
My game features a minimap, fog of war, spells, ai behavior trees, multiple units, and a bunch of other cool components that make up the core of any RTS game.
 
I will break down each of the game's components and implementations throughout the rest of this document. Any long sections will have a TL;DR at the end for brevity!

## Startup Instructions

When opening this project your Unity editor will rebuild the library. The project may default to a new untitled scene. Please load the **Main** scene which is found in Assets/Scenes. From there, you should be able to play the game.
 
## Selection and Input
 
I decided to build a traditional strategy game user input. You can left click to select units. You can also left click and drag to create a selection box overlay that will select all units inside of it from the user's perspective. Holding down the shift key while making selections will add any targeted units to the existing selection. Clicking away from selected units will deselect them all. You can select multiple characters, but only a single structure at any given time.
 
This was all implemented using Raycasting from the camera perspective to the mouse position in world space. The raycast hit is checked and if it hits a friendly unit the selection logic will continue. Selected units are tracked in a List<UnitManager> that is stored on the global Game.cs component, which is a singleton.
 
*TL;DR - Traditional RTS style unit selection built by Raycasting from camera perspective to worldspace mouse position.*
 
## Units
 
Unit’s are broken down into two types; Characters and Structures. Both of these classes have their own unique attributes and functionality, but inherit a tonne of shared behavior from the general Unit class they are children of.
 
Characters are agents that can move around in the world and respond to orders as well as cast abilities. They represent the mobile units that both the player and the enemy can use to attack, defend, and get things done. These units are either spawned through game logic or produced from specific structures.
 
Structures are static units that are built and placed in the world. They serve certain purposes such as gathering nearby resources, producing character units, and defending an area.
 
The data structure/flow for how a unit works goes like this: UnitData >> Unit >> UnitManager.
 
UnitData.cs is a scriptable object that I designed to allow the developer (also me!) to generate unit schematics inside the Unity editor. This means all of the general aspects that units are composed of, such as name, health, speed, vision, range, damage, cost, and what-not, are available to designate in the editor without having to write code for every unique unit. These scriptable objects all allow you to plug in a prefab that represents the units GameObject to be instantiated inside the scene.
 
Unit.cs is the runtime object that is created for each unit. This allows the game logic and UnitManager component a more lightweight object to work with. The constructor for this class takes a few arguments, most important of which is the UnitData scriptable object that supplies all of the vital info.
 
UnitManager.cs is the Monobehaviour script for units. It is attached as a component to their prefab and manages the life cycle, attributes and everything else. It is this script that executes unit movement, attacks, death, and adjustments of any other relevant data.
 
*TL;DR - Units are made from scriptable objects in the unity editor. The Unit class and UnitManager work with the data supplied from the UnitData. My goal was less code based unit design. *
 
## Unit AI Behavior
 
Initially I had all of the unit state and behavior piled inside the unit manager class. This was really cumbersome and required that I write a million little state checks or else figure out some better system. I refined this by building a behavior tree to handle the AI behavior for both enemy units and player controlled units. This works really well in my game because even the player's units need to respond to certain stimuli on their own. The player can’t do everything!
 
Each unit has a BehaviorTree component. A Tree.cs is composed of a root Node.cs and many branched out sub-children. There are many derived classes for the node objects, and each is designed to handle or solve a different kind of behavior or action. The nodes in a tree can have one of three different states after being evaluated: Running, Failed, or Succeeded. These returned states are used to govern a unit's behavior by guiding through the tree. Each root node has a data-context, which is a dictionary that stores key variables. This data context contains values such as the units move destination, attack target, casting ability, and anything else. It’s too much to detail here, but a lot of the logic is covered in [this article](https://www.gamedeveloper.com/programming/behavior-trees-for-ai-how-they-work)
 
*TL;DR - AI is governed by branching behavior tree data structures ie; check this, if true do that.*
 
## Unit Movement
 
For movement I knew I needed some sort of pathfinding system. I looked into building my own, but instead of re-inventing the wheel I decided to leverage Unity’s existing NavMesh system. The system allows you to outline desired objects/terrains as NavMesh spaces that can be traversed by NavMeshAgents.
 
Each Character has a NavMeshAgent component that designates its move speed and other pathfinding attributes. The UnitManager has a host of methods that I wrote to handle working with the NavMeshAgent API such as checking if a position is reachable, calculating paths, stopping or resuming movement, and others.
 
The Characters and Structures also have a NavMeshObstacle component that I toggle on whenever they are stationary or idle. This component allows them to carve out some space on the navigation mesh so that other agents don’t try to path through them. My units use rigid bodies, so this prevents them from forcefully trying to push their way through each other.
 
*TL;DR - Movement works by using Unity’s NavMesh system and writing methods to interact with its API.*
 
## Orders and Targeting
 
The player can give orders to selected units by right clicking on desired targets. A right click on an enemy unit will order selected units to attack it if it is reachable and if they can actually attack. A right click onto the terrain will tell Characters to move there, and Structures to set a waypoint there that any produced units will move to. When casting abilities a targeting reticle will appear and left clicking will attempt to cast the ability if the provided target is legal.
 
## Abilities
 
I wanted my units to have abilities they could cast in a manner similar to other RTS and strategy games. When you select a unit the lower right panel will have a list of buttons that represent the abilities they can perform. Abilities have a variety of properties such as cooldown, range, target type, optional cost, and more.
 
Most of the abilities I designed are simple spells that allow you to attack enemies in more interesting ways. The more powerful spells cost mana. I also designed some defensive spells and some that can buff allies or debuff enemies.
 
When a unit is created it has an AbilityManager component added to its game object for each ability it can cast. These components keep track of the abilities cooldown and ready state. The abilities themselves are created in editor from the AbilityData scriptable object and then assigned to units in their UnitData as a part of a list.
 
I found it hard to design an ability system that allows for an ability to have multiple effects. I solved this by creating an Affect.cs class and then allowing an ability to have a list of affects. To allow effects to be added in-editor I wrote an Affect.Parse() method that can take a string such as: "dot/2/5/5" and generate a damage over time effect that causes 2 damage every 5 seconds 5 times.
 
Abilities also have a bunch of supporting methods in the TargetingManager, AbilityManager, and UnitManager classes that help to check if the targets you are trying to feed into a casting ability are valid.
 
I'm happy with this ability system as it can easily be expanded upon.
 
*TL;DR - Ability system allows you to create spells and actions in the editor that units can have assigned to them and cast in game.*
 
## Resources
 
For this type of game I wanted the player to have to build up an economy to afford the costs required in building their defenses/army. I've designed the game with 4 different resources in mind. Wood, stone, gold, and mana.
 
Wood is used to build most structures as well as certain units. It can be collected by building a Lumber Mill in close proximity to a body of trees. I've placed one large body of trees near the player's starting location, but the rest are scattered deeper into the map.
 
Stone is a requirement for the more advanced and larger structures. It is collected by the Quarry in the same method as wood. but requires a body of stone nearby.
 
Gold is a part of the cost for most units and certain structures. It can be collected by placing a Quarry near a gold vein or by building houses, which provide a small passive stream of income.
 
Mana is used to cast certain spells and is also a requirement for producing certain units. It is produced passively by the mana well and the players main base.
 
*TL;DR Wood, Stone, Gold, and Mana are the resources used to produce units and cast abilities in this game. They are produced by specific structures*
 
## Events
 
I built an EventManager component that tracks and stores listeners in dictionaries of <string, UnityEvent>. This system allows me to trigger events and have any other classes/components that are listening for the triggers to react. It really helped me to keep my classes as separate as possible and reduce their knowledge of each other.
 
## Terrain
 
I used Unity's built in terrain system to create my map. It allowed me to simplify painting textures onto the landscape and altering the elevation at certain points to create hills and impassable terrain.
 
I wanted the map to be obscured to the player in areas where they do not have units. I followed [this tutorial](https://andrewhungblog.wordpress.com/2018/06/23/implementing-fog-of-war-in-unity/) on Andrew Hung's devlog to create a fog of war projection system that works in conjunction with field of few spheres that the player units have attached to them. This makes the map even more immersive and 'scary' for the player to navigate.
 
*Note - My project uses an orthographic camera view which causes a unique and unsolvable bug with Unity's terrain system. Sometimes you will see large square areas of the terrain texture as blurry. I found an existing ticket for this bug, and it is yet to be solved.*
 
## Unit Models
 
I handmade my unit models by using Unity's ProBuilder tool in conjunction with some assets from karboosx's [Mega Fantasy Props Pack](https://assetstore.unity.com/packages/3d/environments/fantasy/mega-fantasy-props-pack-87811). I'm happy with how most of the structures turned out. My character units look ugly as all sin because I wasn't able to build them out of multiple sub-objects due to the plugin I used to create silhouettes for when units are eclipsed by terrain or other objects.
 
## Zombies
 
When I started making this game I wanted it to be a traditional symmetrical RTS game where both you and the AI enemy are trying to build bases and train units to defeat each other. I quickly realized how difficult it would be to write an AI that can manage decision making to that complexity level. Instead, I pivoted to the asymmetrical wave defense game design, where the enemy is just a horde of simple zombies!
 
The zombies are spawned all over the map and in waves. This logic is controlled by my SpawnManager component. I wrote it to support various levels of difficulty through the usage of animation curves and other parameters.
 
The zombies function the same to most other units, except that they have smaller behavior trees and cannot cast abilities.
 
To make them more of a passive threat I added in some wander behavior where a zombie has a chance to head in a random direction to a point it can see.
 
They are not very strong, but when a wave of them is heading for your base they are quite formidable.
 
*TL;DR - Zombies are simple wandering enemy AI units that can be sent at the players in waves or spawned all over the map*
 
## Winning and Losing
 
I've designed the game with one win condition and one loss condition. They are quite simple. If your Wizard's Tower dies, you lose. If you kill all the zombies in the final wave, you win!
 
## Other Features
 
- Projectiles
- Data Loader
- JSON saving
- Minimap
- UI
 
## Performance
 
Having 1000-1500 units in the game seemed to be causing me some performance issues. Initially I couldn't get past 60fps with just 400 zombies. I improved performance with the following changes:
 
- Traverse behavior trees every X seconds/ms instead of every frame
- FogRendererToggler disable unit meshes when obscured by fog of war
- Limit FogRendererToggler check rate to every X seconds/ms
- Cache more variables that units reference repeatedly, where possible
- Rearrange behavior trees so that units can sleep, and only check more complex nodes when awake
 
## Should I Polish This Further?
 
No, I don't think so. I've learnt a tonne while making this process. When I started I didn't have a single clue how to do anything in Unity. Further improvements and polish added to this project would provide diminishing returns. I should have started with a much simpler project. From now on I'll be focusing on making a bunch of smaller games and experiments that allow me to challenge myself and learn how to implement well polished features.