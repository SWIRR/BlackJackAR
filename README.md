# Blackjack AR

Blackjack AR is a mobile augmented reality card game developed in Unity for Android.

The application detects a real-world horizontal surface using the device camera and allows the player to place a virtual Blackjack table directly in their environment.

The game includes full Blackjack mechanics, 3D card animations, an animated dealer and an interface integrated into the AR scene.

## Key Features

- Augmented Reality gameplay using AR Foundation
- Automatic horizontal surface detection
- Virtual Blackjack table placed in the real environment
- Full Blackjack mechanics:
  - Hit
  - Stand
  - Double Down
  - Blackjack
- Betting and player balance system
- Animated 3D dealer
- 3D card and chip animations
- AR-aware card positioning
- Touch controls on Android
- Mouse input support in Unity Editor

## Technical Stack

- Unity
- C#
- AR Foundation
- ARCore XR Plugin
- Unity Input System
- TextMeshPro
- OpenGLES3
- Android

## Project Architecture

The project is divided into several main systems:

- **AR Surface Detection** – detects suitable horizontal surfaces and places the virtual table.
- **Game Logic** – handles deck shuffling, dealing cards, scoring, betting and round resolution.
- **UI System** – manages betting, gameplay and result panels.
- **3D Visualization** – positions and animates cards, chips and the dealer in AR space.
- **Input System** – supports touch input on Android and mouse input in the Unity Editor.

## How to Run

1. Clone the repository.
2. Open the project in Unity.
3. Use a compatible Unity version with AR Foundation and ARCore installed.
4. Build the project for Android.
5. Run it on an ARCore-supported device.
6. Scan a horizontal surface and tap the screen to place the Blackjack table.

## Team

Developed as a university team project by:

- Artur Kuśka
- Paweł Ledwoń
- Szymon Cieślar
- Alan Pawleta
- Marcin Urbańczyk

The project was created for the **Virtual and Augmented Reality Systems** course at the Silesian University of Technology.
