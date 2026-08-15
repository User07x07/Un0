📖 Overview<br>
Un0 is a modern desktop application that brings your favorite movies and series to your fingertips. Built with .NET and WebView2, it provides a seamless browsing experience with built-in ad blocking, automatic updates, and a clean, intuitive interface.  <br>

✨ Features
Feature	Description<br>
🎬 Movie & Series Streaming	Access your personal universe of films and series<br>
🛡️ Ad Blocker	Built-in protection with real-time counter<br>
🎮 Fullscreen Video	Immersive viewing experience<br>
🔄 Auto-Updates	Stay up-to-date with the latest features<br>
📁 File Upload	Drag & drop support for receipts and files<br>
🔐 Google OAuth	Secure authentication<br>
⚡ Fast & Lightweight	Optimized performance<br>

🚀 Quick Start<br>
🔧 Requirements<br>
Windows 10/11 (64-bit)<br>
.NET 6.0 Runtime1<br>
WebView2 Runtime<br>

# Publish as a single executable<br>
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
