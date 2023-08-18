# Venly Dash Project Source

Try it here >> [WEBGL Demo](https://venly.me/venlydash)

## VenlyDash-Project
Venly Dash is a Web3 adaptation of Unity's Endless Runner [Trash Dash Sample](https://github.com/Unity-Technologies/EndlessRunnerSampleGame) project.

Playable characters and their accessories are represented as NFTs and are usable inside the game if the player owns the corresponding tokens (wallet).
The game also exposes a simple store that allows the user to buy new NFTs (minting) with Coins earned by playing the game.

A classic Web2 login/signup flow is used while leveraging the VenlyAPI to attach Web3 functionalities (Wallet/Tokens) to the game.

### Features

* DevMode (Editor Only)
* Identity Service (PlayFab/Beamable)
* Leaderboard (PlayFab/Beamable)
* Wallet Creation
* Token Retrieval
* Token Minting
* Token Transferring
* WebGL, Mobile, Standalone

## VenlyDash-Azure
This project contains the Azure Function code required to use PlayFab as a Backend with the VenlyAPI. Next to the basic API communication it also contains ExtensionRoutes to handle certain gameplay flows which should be executed from the backend for security reasons (claiming tokens).
