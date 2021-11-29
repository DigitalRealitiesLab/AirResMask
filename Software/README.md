# Software

## Electronics

We use the [ESP32 library](https://github.com/espressif/arduino-esp32) of Espressif for programming the board.
In the folder ''Electronics'' you can find a file to program the circuit board, as well as the [ESP32Servo](https://github.com/madhephaestus/ESP32Servo) library. To upload the software to the board you need a SH-U09C2 or similar USB adapter.

You can find more ways to read and process SFML3300 sensor data [here](https://github.com/MyElectrons/sfm3300-arduino).

## UnitySample

This folder contains a simple Unity sample that demonstrates a version of the breathing engine where sensor values are received, transformed to forces to apply to the scene and breathing resistance can be set. To reduce the file size, we removed dependencies to the Oculus SDK. 

Additional 3rd party assets can be retrieved from the Unity Asset store:
[Arduino Bluetooth Plugin](https://assetstore.unity.com/packages/tools/input-management/arduino-bluetooth-plugin-98960)