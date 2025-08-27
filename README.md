# Clutter: Bluetooth LE Group Chat

Clutter is an experimental project built with **.NET MAUI** that attempts to implement a **Bluetooth Low Energy (BLE) group chat** system.

> ⚠️ Note: This is a work-in-progress. There are known bugs related to device disconnection and reconnection. General messaging functionality works.

## Features

- Discover nearby BLE devices using a custom service UUID.
- Connect and maintain connections to multiple devices.
- Send and receive messages in a group chat format.
- Simple system notifications for device connection/disconnection.
- Plays sound notifications for new messages and discoveries.

## Limitations / Known Issues

- Devices occasionally fail to reconnect automatically after disconnection.
- Message delivery may fail if MTU negotiation fails or connections are lost.
- Debugging logs are printed to the console for error tracking.

## Technology Stack

- [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le) for cross-platform BLE functionality.
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) for MVVM support.
- Android-specific BLE server implementation.

---

