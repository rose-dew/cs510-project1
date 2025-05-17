[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/ja_78tnU)
# Todo Weather Web App

![App Screenshot](screenshot.png) <!-- Add a screenshot later -->

A simple yet powerful todo list application built with F# and Giraffe, featuring weather data integration and email notifications.

## Features

- ✅ **Todo List Management**
  - Add, complete, and delete tasks
  - HTMX for dynamic updates without page reloads
- ⛅ **Weather Integration**
  - Real-time weather data from Open-Meteo API
  - Automatic location-based forecasts
- ✉️ **Email Notifications**
  - Daily digest at 6:00 AM
  - Includes todos and weather forecast
- 🖼️ **Daily Inspirational Images**
  - Beautiful random images from Unsplash

## Technologies

- **Backend**: F#, Giraffe, ASP.NET Core
- **Frontend**: HTMX, Bootstrap 5
- **APIs**: Open-Meteo (weather), Unsplash (images)
- **Infrastructure**: SMTP email delivery

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- An email account for notifications (Gmail recommended)
- [Unsplash Access Key](https://unsplash.com/developers)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/project-1-web-app-rose-dew.git
   cd PLCproj
