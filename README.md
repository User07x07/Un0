📖 Overview
Un0 is a modern desktop application that brings your favorite movies and series to your fingertips. Built with .NET and WebView2, it provides a seamless browsing experience with built-in ad blocking, automatic updates, and a clean, intuitive interface.  

✨ Features
Feature	Description
🎬 Movie & Series Streaming	Access your personal universe of films and series
🛡️ Ad Blocker	Built-in protection with real-time counter
🎮 Fullscreen Video	Immersive viewing experience
🔄 Auto-Updates	Stay up-to-date with the latest features
📁 File Upload	Drag & drop support for receipts and files
🔐 Google OAuth	Secure authentication
⚡ Fast & Lightweight	Optimized performance

🚀 Quick Start
🔧 Requirements
Windows 10/11 (64-bit)
.NET 6.0 Runtime1
WebView2 Runtime

# Publish as a single executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
