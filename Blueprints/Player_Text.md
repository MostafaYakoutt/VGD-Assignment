# Left Behind: every piece of text the player sees

Pulled from the scenes, the dialogue assets in `Assets/Story/Dialogue` and the UI scripts. Walter's thoughts (Inner voice) show in italics with no name; Remembered lines show the speaker's name and are faded, in quotation marks. Every interaction prompt is shown with `[E]` in front of it.

## Menus and interface

**Main menu**
- Title: LEFT BEHIND
- Buttons: Begin · Chapters · Quit
- Chapters list: Chapter I · Chapter II · Chapter III (locked ones show `—  —  —`) · Back
- Controls hint: WASD move · Mouse look · E interact · F flashlight · Tab pockets · Esc pause
- Photo caption (only after the Remember ending): "Walter, Ruth, Emily and Sam."

**Pause menu**
- Paused
- Buttons: Resume · Restart chapter · Main menu · Quit
- Hint: Restart chapter goes back to the start of this chapter.

**Pockets (Tab)**
- POCKETS · Carrying · Notes
- Empty states: "Nothing. Empty pockets." · "No notes yet."
- Hint: [Tab] Close      [1-9] Read a note again

**Note reader**: hint "[E] Put it down"

**HUD hints**
- `[E] <prompt>` when looking at something usable
- "[F] Flashlight" (after the flashlight is picked up)
- "Not yet." (the upstairs door, when it has no gate dialogue)
- Dialogue choices show as "1.  <choice>", "2.  <choice>"

**Transition between chapters**: no text (the VHS rewind plays with no label or caption).

**Endings**
- Remember: "He remembered their names." then "Ruth.   Emily.   Sam.", then LEFT BEHIND and "Press any key"
- Look away: the rewind plays (no text) and the game starts over at Chapter I

## Chapter I

**Title cards**: LEFT BEHIND

**Objectives**
- Go upstairs.
- Find another way out.
- Get out of the house.

**Notes and things to read**
- **Calendar on the fridge** (prompt: "Read the calendar")

  ```
  X X X X X
  X X X X X X X
  X X X X X X X
  X X X X X X X
  X X X X X
  
  WHAT AM I COUNTING DOWN TO?
  ```

**Items picked up (shown in Pockets)**
- **Wedding ring**: My wedding ring. It won't go over the knuckle any more. Engraved inside: R & W.
- **Flashlight**: Ruth bought it for the winter power cuts. She always kept spare batteries in the kitchen drawer.

**Interaction prompts**: Take the ring · Turn on lamp · Turn off lamp · Open door · Close door · Locked · Look at the pills · Open the door · It won't open · Inspect · Put back · Read the calendar · Use the tap · Turn on the tap · Turn off the tap · Turn the tap off tight · Look at the bear · Look at the clock · Pick up flashlight · Open the front door

**Dialogue**

**L1_Calendar**  _(from: FridgeCalendar)_

- *Walter (thought)*: "Every day crossed off."
- *Walter (thought)*: "Crossed off until what?"

**L1_Clock**  _(from: Beat_Clock)_

- *Walter (thought)*: "Twenty to four."
- *Walter (thought)*: "In the afternoon? It gets dark so early now, I can never tell."

**L1_EmptyTable**  _(from: Beat_EmptyTable)_

- *Walter (thought)*: "One chair. One plate."
- *Walter (thought)*: "When did I start eating alone?"

**L1_FamilyPhoto**  _(from: Beat_FamilyPhoto)_

- *Walter (thought)*: "Ruth. And the children."
- *Walter (thought)*: "Why can't I see their faces?"

**L1_Flashlight**  _(from: Beat_Flashlight)_

- *Walter (thought)*: "Ruth's flashlight. She bought it for the power cuts."
- *Walter (thought)*: "Upstairs, then. There has to be another way out."

**L1_FrontDoor**  _(from: Beat_FrontDoor)_

- *Walter (thought)*: "It won't open."
- *Walter (thought)*: "It isn't locked. It just... won't."

**L1_Intro**  _(from: Beat_Intro)_

- *Walter (thought)*: "The front door. I was going out... wasn't I?"
- *Walter (thought)*: "It's so quiet. Where is everyone?"

**L1_LoopDoorNotYet**  _(from: LoopDoor)_

- *Walter (thought)*: "Not yet. I've missed something downstairs."

**L1_MannequinMoved**  _(from: Beat_MannequinMoved)_

- *Walter (thought)*: "That one wasn't there a moment ago."
- *Walter (thought)*: "...Was it?"

**L1_Mannequins**  _(from: Beat_Mannequins)_

- *Walter (thought)*: "Ruth's mannequins, from her sewing room."
- *Walter (thought)*: "Why are they all up here, facing the door?"

**L1_Pills**  _(from: Beat_Pills)_

- *Walter (thought)*: "Monday, Tuesday, Wednesday... they're all still full."
- *Walter (thought)*: "Have I taken them today? What is today?"

**L1_Ring**  _(from: WeddingRing)_

- *Walter (thought)*: "My ring. When did I take it off?"
- *Walter (thought)*: "R and W. Ruth and Walter."

**L1_Tap**  _(from: Beat_Tap)_

- *Walter (thought)*: "It still drips."
- *Walter (thought)*: "I keep meaning to have someone fix it. Don't I?"

**L1_Teddy**  _(from: Beat_Teddy)_

- *Walter (thought)*: "A child's bear, packed away with the toys."
- *Walter (thought)*: "Whose was it? Why can't I remember?"

## Chapter II

**Objectives**
- Leave. Now.
- Open the study.
- Find the key to the study.
- Restore the power.
- Go upstairs.
- Find Ruth.

**Notes and things to read**
- **Note on the sink** (prompt: "Read the note")

  ```
  TURN THE TAP
  ALL THE WAY.
  ```
- **Note on the kitchen table** (prompt: "Read the note")

  ```
  SET THE TABLE
  FOR EVERYONE.
  
  THEY MIGHT
  COME BACK TONIGHT.
  ```
- **Note on the fridge** (prompt: "Read the note")

  ```
  RUTH TOOK THE KIDS
  TO HER MOTHER'S.
  
  DON'T CALL.
  
  — W.
  ```
- **Calendar on the fridge** (prompt: "Read the calendar")

  ```
  Fri 6 — THEY COME BACK?
  Fri 13 — STAY OUT OF THE STUDY
  Fri 20 — THEY COME BACK?
  Fri 27 — THEY COME BACK?
  
  EVERY SUNDAY: WIND THE CLOCK.
  RUTH ALWAYS DID.
  ```
- **Note on the fuse box** (prompt: "Read the note")

  ```
  SPARE FUSE:
  DINING ROOM SIDEBOARD.
  ```
- **Note by the front door** (prompt: "Read the note")

  ```
  KEYS ARE IN YOUR COAT.
  YOUR COAT IS ON.
  ```
- **Note on a cupboard** (prompt: "Read the note")

  ```
  OVEN OFF.
  CHECK TWICE.
  ```
- **Note on a drawer** (prompt: "Read the note")

  ```
  YOU HAVE ALREADY
  EATEN TODAY.
  ```

**Items picked up (shown in Pockets)**
- **Study key**: A small brass key, wrapped in thread at the bottom of Ruth's sewing box. She kept it from me.
- **Spare fuse**: A ceramic fuse from the sideboard. Ruth always kept one spare, just in case.
- **Ruth's ring**: Ruth's wedding ring, left on the dresser. She never takes it off, not even to sleep.

**Interaction prompts**: Open Ruth's sewing box · Turn on lamp · Turn off lamp · Unlock the study · Locked · Open door · Close door · Read the note · Open the fuse box · Fit the spare fuse · Take the spare fuse · Read the calendar · Look at the bear · Take the ring · Look at the drawing · Look at the clock · Open the door · It won't open · Look at the pills · Use the tap · Turn on the tap · Turn off the tap · Turn the tap off tight · Open the front door

**Dialogue**

**L2_Arrival**  _(from: Beat_Arrival)_

- **RUTH** (remembered): "Walter? Dinner's on the table."
- *Walter (thought)*: "Ruth? ...Ruth!"

**L2_Calendar**  _(from: FridgeCalendar)_

- *Walter (thought)*: "Every Friday. They might come back on a Friday."
- *Walter (thought)*: "And the thirteenth, circled twice. Why that day?"

**L2_Clock**  _(from: Beat_Clock)_

- *Walter (thought)*: "Ten to seven. Dinner's at seven."
- *Walter (thought)*: "She'll be back by seven. She always is."

**L2_DarkLanding**  _(from: Beat_DarkLanding)_

- *Walter (thought)*: "It's dark up here. The lights are out."
- *Walter (thought)*: "The fuse again. Sam hates the dark."

**L2_Drawing**  _(from: Beat_Drawing)_

- **EMILY** (remembered): "Look, Daddy! It's all of us, in front of the house."
- *Walter (thought)*: "She always drew us holding hands."

**L2_FridgeNote**  _(from: ReminderNote_Fridge)_

- *Walter (thought)*: "That's my handwriting."
- *Walter (thought)*: "Why would I write that? Why can't I remember writing it?"

**L2_FrontDoor**  _(from: Beat_FrontDoor)_

- *Walter (thought)*: "Still won't open."
- *Walter (thought)*: "She'll be back soon. She always comes back."

**L2_FuseBoxNoFuse**  _(from: FuseBox)_

- *Walter (thought)*: "One of them has blown."
- *Walter (thought)*: "There's a spare somewhere. I wrote it down."

**L2_FuseFitted**  _(from: FuseBox)_

- *Walter (thought)*: "There."
- **SAM** (remembered): "Dad, leave the landing light on?"

**L2_LoopDoorNotYet**  _(from: LoopDoor)_

- *Walter (thought)*: "Not yet."
- *Walter (thought)*: "Not until I know what's in the study."

**L2_Pills**  _(from: Beat_Pills)_

- *Walter (thought)*: "From the memory clinic. 'One each morning.' It's never been opened."
- *Walter (thought)*: "Ruth wanted me to go, so I went. There's nothing wrong with me."

**L2_Ring**  _(from: WeddingRing)_

- *Walter (thought)*: "Ruth's ring."
- *Walter (thought)*: "She went to her mother's and left her ring behind?"

**L2_StudyBricked**  _(from: Door_Study_Locked)_

- *Walter (thought)*: "Bricks."
- *Walter (thought)*: "Fresh bricks. Somebody walled it up."
- *Walter (thought)*: "I did. Why would I do that?"

**L2_StudyKey**  _(from: SewingBox)_

- **RUTH** (remembered): "Not tonight, Walter. Leave the study alone."
- *Walter (thought)*: "The study key. Why did she hide it?"

**L2_StudyLocked**  _(from: Door_Study_Locked)_

- *Walter (thought)*: "Locked. I never lock it."

**L2_TableForSix**  _(from: Beat_TableForSix)_

- *Walter (thought)*: "Six places."
- *Walter (thought)*: "There are only four of us."

**L2_TakeFuse**  _(from: SpareFuse)_

- *Walter (thought)*: "The spare fuse. Right where the note said."

**L2_Tap**  _(from: Beat_Tap)_

- *Walter (thought)*: "All the way, like the note says."
- *Walter (thought)*: "Ruth used to say the dripping kept her awake."

**L2_Teddy**  _(from: Beat_Teddy)_

- *Walter (thought)*: "Sam's bear, on his pillow."
- *Walter (thought)*: "He'll want it when they get back."

## Chapter III

**Objectives**
- Open the study.
- Find the children.
- Find Ruth.

**Notes and things to read**
- **Ruth's calendar** (prompt: "Read the calendar")

  ```
  Fri 1 — Sam, swimming 5pm
  Fri 8 — Emily's school play, 6pm
  Sat 16 — Mum's birthday. Take the kids.
  Mon 18 — Sam, dentist 4.15
  Tue 19 — Walter, memory clinic 10.30
  
  DON'T LET HIM CANCEL.
  ```
- **Note on the study door** (prompt: "Read the note")

  ```
  THAT WOMAN IS NOT RUTH.
  
  She has Ruth's face and Ruth's voice, but she watches me. The children are not mine. They whisper when I leave the room.
  
  They want me to forget.
  I will not forget.
  
  — W.
  ```
- **Card on the fridge** (prompt: "Read the card")

  ```
  ST. MARY'S MEMORY CLINIC
  
  Appointment for: Walter Hale
  Tuesday, 10:30
  
  Please bring a family member with you.
  ```

**Interaction prompts**: Look at the clock · Turn on lamp · Turn off lamp · Open door · Close door · Locked · Look at the drawing · Read the calendar · Look at the knife block · Look at the bear · Open the door · Use the tap · Turn on the tap · Turn off the tap · Turn the tap off tight · Inspect · Put back · Read the note · Read the card · Open the study · Open the front door

**Dialogue**

**L3_Arrival**  _(from: Beat_Arrival)_

- *Walter (thought)*: "Every light in the house is on."
- *Walter (thought)*: "Ruth? Where are you?"

**L3_Calendar**  _(from: FridgeCalendar)_

- *Walter (thought)*: "Ruth's writing."
- *Walter (thought)*: "Her mother's birthday on Saturday. The clinic on Tuesday. She had it all planned."

**L3_ClinicCard**  _(from: ClinicCard)_

- *Walter (thought)*: "Ruth booked this for me."
- *Walter (thought)*: "She knew. She knew something was wrong with me."

**L3_Clock**  _(from: Beat_Clock)_

- *Walter (thought)*: "It's stopped. Eight minutes to midnight."
- *Walter (thought)*: "Ruth winds it every Sunday. She'd never let it stop."

**L3_DelusionNote**  _(from: StudyDoorNote)_

- *Walter (thought)*: "My handwriting."
- *Walter (thought)*: "No. That isn't... I didn't..."

**L3_Drawing**  _(from: Beat_Drawing)_

- **EMILY** (remembered): "That's you, Daddy. I couldn't get your face right."
- *Walter (thought)*: "She scribbled it out."

**L3_FrontDoor**  _(from: Beat_FrontDoor)_

- *Walter (thought)*: "Locked. I locked it myself."
- *Walter (thought)*: "Why would I lock us in?"

**L3_KnifeBlock**  _(from: Beat_KnifeBlock)_

- *Walter (thought)*: "One of the knives is missing."
- *Walter (thought)*: "Ruth never leaves them out."

**L3_NoLoopDoor**  _(from: Beat_NoLoopDoor)_

- *Walter (thought)*: "It won't open."
- *Walter (thought)*: "There's no way out of tonight."

**L3_Study**  _(from: Beat_Study)_

- *Walter (thought)*: "The study."
- *Walter (thought)*: "The knife from the kitchen."
- *Walter (thought)*: "Ruth. Emily. Sam."
- *Walter (thought)*: "I could remember. Or I could close the door and forget, again."
- Choice 1: **Remember.** → `L3_Remember`
- Choice 2: **Look away.** → `L3_LookAway`

**L3_Remember**  _(after choosing "Remember.")_

- *Walter (thought)*: "Ruth. Emily. Sam."
- *Walter (thought)*: "I remember now. I remember all of it."

**L3_LookAway**  _(after choosing "Look away.")_

- *Walter (thought)*: "No."
- *Walter (thought)*: "I don't want to know."

**L3_StudyLocked**  _(from: Door_Study)_

- *Walter (thought)*: "Locked."
- *Walter (thought)*: "There's something taped to the door."

**L3_Tap**  _(from: Beat_Tap)_

- *Walter (thought)*: "Who left the tap running?"
- *Walter (thought)*: "Somebody's been washing something."

**L3_Teddy**  _(from: Beat_Teddy)_

- *Walter (thought)*: "Sam's bear."
- *Walter (thought)*: "He never goes anywhere without it."

