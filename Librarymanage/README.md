# Library Management System

## Project Description
This is a desktop Library Management System built using WPF (.NET) with C#. The application allows users to browse, search, and reserve books. It also provides an admin interface for managing books, members, and viewing statistics. The system uses a SQLite database for data storage and integrates with the Google Books API to fetch book cover images and details.

## Features
- User authentication with Login and Signup pages.
- Browse and search books in the library.
- View detailed book information including cover image, author, release date, ISBN, and summary.
- Reserve books with selectable reservation and return dates.
- Admin dashboard with statistics on new members, new books, and reservations.
- Admin book management: add books (via Google Books API), modify, and delete books.
- Event and member management placeholders in admin panel.
- Data persistence using SQLite database.
- Interactive charts for statistics using LiveCharts.

## Technologies Used
- C# with WPF for desktop UI.
- SQLite for local database storage.
- Google Books API for fetching book details and cover images.
- LiveCharts for displaying statistics.
- Newtonsoft.Json for JSON parsing.

## Setup and Running Instructions
1. Ensure you have .NET 8.0 SDK or later installed on your Windows machine.
2. Clone or download the project repository.
3. Open the solution file `Librarymanage.sln` in Visual Studio.
4. Restore NuGet packages if needed.
5. Build the solution.
6. Run the application from Visual Studio or by executing the generated `.exe` in the `bin/Debug/net8.0-windows` folder.
7. The main window will open with navigation to Home page where you can login or sign up.
8. Admin users can access the admin panel for management features.

## Project Structure
- `MainWindow.xaml(.cs)`: Main application window with frame navigation.
- `Home_Win.xaml(.cs)`: Home page with login and signup navigation.
- `Login_Page.xaml(.cs)`: User login page.
- `SignUp_Page.xaml(.cs)`: User signup page.
- `Library_Page.xaml(.cs)`: User library page for browsing and reserving books.
- `Admin_Page.xaml(.cs)`: Admin dashboard for managing books, members, events, and viewing statistics.
- `Data/Library.db`: SQLite database file storing users, books, reservations, etc.
- `Images/`: Contains book cover images and other UI images.
- `bin/Debug/net8.0-windows/`: Compiled binaries and dependencies.

## Notes
- The application requires internet access to fetch book cover images from the Google Books API.
- The database file is located in the `Data` folder and is created/updated automatically.
- Admin features require appropriate user roles (not detailed in the current code).

## Author
This project was developed as a library management desktop application using WPF and SQLite.
