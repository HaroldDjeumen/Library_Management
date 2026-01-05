using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Librarymanage
{
    /// <summary>
    /// Interaction logic for Admin_Page.xaml
    /// </summary>
    public partial class Admin_Page : Page
    {
        private Frame _mainFrame;
        private static string connectionString = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Library.db")};Version=3;";

        private ObservableCollection<User> UsersList = new ObservableCollection<User>();

        public Func<double, string> Formatter { get; set; }
        public SeriesCollection NewMembersSeries { get; set; }
        public SeriesCollection NewBooksSeries { get; set; }
        public SeriesCollection BookReservationsSeries { get; set; }
        public List<string> Labels { get; set; }



        public Admin_Page(Frame mainFrame)

        {
            InitializeComponent();
            _mainFrame = mainFrame;
            LoadUsersFromDatabase();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("PRAGMA journal_mode=WAL;", connection))
                {
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }

        }

        private void BooksButton_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            StatAdminPanel.Visibility = Visibility.Collapsed;
            BookAdminPanel.Visibility = Visibility.Visible;
            ContentPanel.Children.Add(BookAdminPanel);
            LoadBooksFromDatabase();
        }

        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            BookAdminPanel.Visibility = Visibility.Collapsed;
            StatAdminPanel.Visibility = Visibility.Collapsed;
            EventsAdminPanel.Visibility = Visibility.Collapsed;
            MembersAdminPanel.Visibility = Visibility.Visible;
            ContentPanel.Children.Add(MembersAdminPanel);
        }

        private void EventsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            BookAdminPanel.Visibility = Visibility.Collapsed;
            StatAdminPanel.Visibility = Visibility.Collapsed;
            MembersAdminPanel.Visibility = Visibility.Collapsed;
            EventsAdminPanel.Visibility = Visibility.Visible;
            ContentPanel.Children.Add(EventsAdminPanel);
        }
        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            BookAdminPanel.Visibility = Visibility.Collapsed;
            StatAdminPanel.Visibility = Visibility.Visible;
            MembersAdminPanel.Visibility = Visibility.Collapsed;
            EventsAdminPanel.Visibility = Visibility.Collapsed;
            ContentPanel.Children.Add(StatAdminPanel);
            LoadStatisticsData();
        }

        private async void AdminSearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = AdminSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            ApiBooksPanel.Children.Clear();

            List<Book> books = await SearchBooksFromAPI(query);


            foreach (var book in books)
            {
                Border bookBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(10)
                };

                StackPanel bookPanel = new StackPanel { Orientation = Orientation.Horizontal };

                // Book Cover Image
                Image coverImage = new Image
                {
                    Width = 120,
                    Height = 160,
                    Margin = new Thickness(0, 0, 10, 0),
                    Source = new BitmapImage(new Uri(book.ImagePath, UriKind.RelativeOrAbsolute))
                };

                StackPanel detailsPanel = new StackPanel { Width = 500 };

                // Book Details
                TextBlock title = new TextBlock { Text = book.Title, FontWeight = FontWeights.Bold, FontSize = 18 };
                TextBlock author = new TextBlock { Text = $"Author: {book.Author}", Margin = new Thickness(0, 5, 0, 0) };
                TextBlock release = new TextBlock { Text = $"Release Date: {book.ReleaseDate}", Margin = new Thickness(0, 5, 0, 0) };

                // ✅ Show if Preview is available
                TextBlock previewStatus = new TextBlock
                {
                    Text = string.IsNullOrEmpty(book.PreviewUrl) ? "Preview: Not Available" : "Preview: Available",
                    Margin = new Thickness(0, 5, 0, 0),
                    Foreground = new SolidColorBrush(string.IsNullOrEmpty(book.PreviewUrl) ? Colors.Red : Colors.Green)
                };

                // Book Description inside a ScrollViewer
                ScrollViewer descriptionScroll = new ScrollViewer
                {
                    Height = 100, // limit display height
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto // hidden until needed
                };

                TextBlock description = new TextBlock
                {
                    Text = book.Summary,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                descriptionScroll.Content = description;

                Button addButton = new Button
                {
                    Content = "Add to Database",
                    Margin = new Thickness(0, 10, 0, 0),
                    Tag = book,
                };
                addButton.Click += AddBookToDatabase;

                // Build details panel
                detailsPanel.Children.Add(title);
                detailsPanel.Children.Add(author);
                detailsPanel.Children.Add(release);
                detailsPanel.Children.Add(previewStatus); // ✅ Added preview info here
                detailsPanel.Children.Add(descriptionScroll);
                detailsPanel.Children.Add(addButton);

                // Build book panel
                bookPanel.Children.Add(coverImage);
                bookPanel.Children.Add(detailsPanel);

                bookBorder.Child = bookPanel;

                ApiBooksPanel.Children.Add(bookBorder);
            }
        }


        private async void AddBookToDatabase(object sender, RoutedEventArgs e)
        {
            Button? addButton = sender as Button;

            Book selectedBook = (Book)addButton.Tag;

            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string insertQuery = @"INSERT INTO Books 
                                     (Name, [Release-Date], Author, ISBN, Description, JoinDate, BookImage, PreviewUrl) 
                                     VALUES 
                                     (@Name, @ReleaseDate, @Author, @ISBN, @Description, @JoinDate, @BookImage, @PreviewUrl)";

                using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                {
                    // Parse release date
                    DateTime releaseDate;
                    string[] formats = { "yyyy-MM-dd", "yyyy", "d/M/yyyy", "dd/MM/yyyy", "M/d/yyyy", "yyyy-MM" };

                    if (DateTime.TryParseExact(selectedBook.ReleaseDate.Trim(), formats,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out releaseDate))
                    {
                        cmd.Parameters.AddWithValue("@ReleaseDate", releaseDate.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        MessageBox.Show($"Invalid release date format for book '{selectedBook.Title}'.");
                        return;
                    }

                    cmd.Parameters.AddWithValue("@Name", selectedBook.Title);
                    cmd.Parameters.AddWithValue("@Author", selectedBook.Author);
                    cmd.Parameters.AddWithValue("@ISBN", selectedBook.ISBN);
                    cmd.Parameters.AddWithValue("@Description", selectedBook.Summary);
                    cmd.Parameters.AddWithValue("@JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));

                    // ✅ Download and attach the image
                    byte[]? imageBytes = await DownloadImageBytesAsync(selectedBook.ImagePath);
                    if (imageBytes != null)
                        cmd.Parameters.AddWithValue("@BookImage", imageBytes);
                    else
                        cmd.Parameters.AddWithValue("@BookImage", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreviewUrl", selectedBook.PreviewUrl ?? (object)DBNull.Value);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        LoadBooksFromDatabase();
                        conn.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding book: {ex.Message}");
                    }
                }
            }
        }


        private async Task<byte[]?> DownloadImageBytesAsync(string imageUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(imageUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsByteArrayAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading image: {ex.Message}");
            }

            return null;
        }

        private async Task<List<Book>> SearchBooksFromAPI(string searchQuery)
        {
            ApiBooksPanel.Children.Clear();
            List<Book> books = new List<Book>();
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"https://openlibrary.org/search.json?q={searchQuery.Replace(" ", "+")}&limit=40";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                        return books;

                    var jsonString = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(jsonString);

                    if (data.docs != null)
                    {
                        foreach (var doc in data.docs)
                        {
                            bool hasEbook = doc.ebook_access == "full" || doc.has_fulltext == true;
                            if (!hasEbook) continue;

                            string title = doc.title != null ? (string)doc.title : "Unknown Title";
                            string author = (doc.author_name != null && doc.author_name.Count > 0)
                                ? string.Join(", ", doc.author_name.ToObject<string[]>())
                                : "Unknown Author";

                            string imagePath = doc.cover_i != null
                                ? $"https://covers.openlibrary.org/b/id/{doc.cover_i}-L.jpg"
                                : "https://via.placeholder.com/150";

                            // ✅ direct ebook link if possible
                            string previewUrl = "no preview available";
                            if (doc.ia != null && doc.ia.Count > 0)
                            {
                                string identifier = doc.ia[0];
                                previewUrl = $"https://archive.org/stream/{identifier}";
                            }
                            else if (doc.key != null)
                            {
                                previewUrl = $"https://openlibrary.org{doc.key}";
                            }

                            // Defaults
                            string isbn = "Unknown";
                            string summary = "No description available";

                            // Google fallback for ISBN + summary
                            var googleData = await FetchGoogleBookData(title, author);
                            if (!string.IsNullOrEmpty(googleData.isbn))
                                isbn = googleData.isbn;
                            if (!string.IsNullOrEmpty(googleData.description))
                                summary = googleData.description;

                            books.Add(new Book
                            {
                                Title = title,
                                Author = author,
                                ReleaseDate = doc.first_publish_year != null ? doc.first_publish_year.ToString() : "Unknown",
                                ISBN = isbn,
                                ImagePath = imagePath,
                                Summary = summary,
                                PreviewUrl = previewUrl
                            });

                            if (books.Count >= 40) break;
                        }

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching data from Open Library: {ex.Message}");
                }
            }

            return books;
        }

        private async Task<(string isbn, string description)> FetchGoogleBookData(string title, string author)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string query = $"{title} {author}".Replace(" ", "+");
                    string url = $"https://www.googleapis.com/books/v1/volumes?q={query}&maxResults=1";

                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return (null, null);

                    var jsonString = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(jsonString);

                    if (data.items != null && data.items.Count > 0)
                    {
                        var volumeInfo = data.items[0].volumeInfo;

                        // ISBN
                        string isbn = null;
                        if (volumeInfo.industryIdentifiers != null)
                        {
                            foreach (var id in volumeInfo.industryIdentifiers)
                            {
                                if ((string)id.type == "ISBN_13")
                                {
                                    isbn = id.identifier;
                                    break;
                                }
                                if ((string)id.type == "ISBN_10" && isbn == null)
                                {
                                    isbn = id.identifier;
                                }
                            }
                        }

                        // Description
                        string description = volumeInfo.description != null
                            ? (string)volumeInfo.description
                            : null;

                        return (isbn, description);
                    }
                }
                catch
                {
                    return (null, null);
                }
            }

            return (null, null);
        }


        private void LoadBooksFromDatabase()
        {
            DatabaseBooksPanel.Children.Clear();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Books";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        StackPanel bookRow = new StackPanel { Orientation = Orientation.Horizontal};

                        // Show book name and author
                        TextBlock bookInfo = new TextBlock
                        {
                            Text = $"{reader["Name"]} by {reader["Author"]}, added on {reader["JoinDate"]}",
                            Width = 520,
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 16
                        };

                        // Delete button
                        Button deleteButton = new Button
                        {
                            Content = "Delete",
                            Margin = new Thickness(5),
                            Background = Brushes.Red,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        int bookId = Convert.ToInt32(reader["Id"]);
                        deleteButton.Click += (s, e) => DeleteBook(bookId);

                        // Modify button
                        Button modifyButton = new Button
                        {
                            Content = "Modify",
                            Margin = new Thickness(5,5,0,5),
                            Background = Brushes.Orange,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        modifyButton.Click += (s, e) => ModifyBook(bookId);

                        // Add controls
                        bookRow.Children.Add(bookInfo);
                        bookRow.Children.Add(deleteButton);
                        bookRow.Children.Add(modifyButton);

                        DatabaseBooksPanel.Children.Add(bookRow);
                    }
                }
            }
        }

        private void DeleteBook(int bookId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Books WHERE Id = @Id";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", bookId);
                    command.ExecuteNonQuery();
                }
            }

           // MessageBox.Show("Book deleted.");
            LoadBooksFromDatabase(); // Refresh list
        }

        private void ModifyBook(int bookId)
        {
            // Find the book data
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Books WHERE Id = @Id";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", bookId);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string? name = reader["Name"].ToString();
                            string? author = reader["Author"].ToString();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            string? releaseDate = reader["Release-Date"].ToString().Trim();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            string? isbn = reader["ISBN"].ToString();
                            string? description = reader["Description"].ToString();


                            // Show an input form
#pragma warning disable CS8604 // Possible null reference argument.
                            ShowModifyForm(bookId, name, author, releaseDate, isbn, description);
#pragma warning restore CS8604 // Possible null reference argument.
                        }
                    }
                }
            }
        }

        private void ShowModifyForm(int bookId, string name, string author, string releaseDate, string isbn, string description)
        {
            Window editWindow = new Window
            {
                Title = "Edit Book",
                Width = 450,
                Height = 600,
                Background = Brushes.White,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20), VerticalAlignment = VerticalAlignment.Center };

            TextBlock CreateLabel(string text) => new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5),
                Foreground = Brushes.Black
            };

            TextBox nameBox = new TextBox { Text = name, Height = Double.NaN, Margin = new Thickness(0, 0, 0, 10) };
            TextBox authorBox = new TextBox { Text = author, Height = Double.NaN, Margin = new Thickness(0, 0, 0, 10) };
            TextBox releaseDateBox = new TextBox { Text = releaseDate, Height = Double.NaN, Margin = new Thickness(0, 0, 0, 10) };
            TextBox isbnBox = new TextBox { Text = isbn, Height = Double.NaN, Margin = new Thickness(0, 0, 0, 10) };
            TextBox descriptionBox = new TextBox { Text = description, Height = 100, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };

            Button saveButton = new Button
            {
                Content = "Save",
                Width = 150,
                Height = 40,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B0082")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            saveButton.Click += async (s, e) =>
            {
                UpdateBook(bookId, nameBox.Text, authorBox.Text, releaseDateBox.Text, isbnBox.Text, descriptionBox.Text);
                await Task.Delay(200); // Small delay to let database finish writing
                LoadBooksFromDatabase(); // Now refresh
                editWindow.Close();
            };

            panel.Children.Add(CreateLabel("Name:"));
            panel.Children.Add(nameBox);
            panel.Children.Add(CreateLabel("Author:"));
            panel.Children.Add(authorBox);
            panel.Children.Add(CreateLabel("Release Date:"));
            panel.Children.Add(releaseDateBox);
            panel.Children.Add(CreateLabel("ISBN:"));
            panel.Children.Add(isbnBox);
            panel.Children.Add(CreateLabel("Description:"));
            panel.Children.Add(descriptionBox);
            panel.Children.Add(saveButton);

            editWindow.Content = panel;
            editWindow.ShowDialog();
        }


        private void UpdateBook(int bookId, string name, string author, string releaseDate, string isbn, string description)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"UPDATE Books 
                         SET Name = @Name, Author = @Author, `Release-Date` = @ReleaseDate, ISBN = @ISBN, Description = @Description
                         WHERE Id = @Id";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Author", author);
                    command.Parameters.AddWithValue("@ReleaseDate", releaseDate);
                    command.Parameters.AddWithValue("@ISBN", isbn);
                    command.Parameters.AddWithValue("@Description", description);
                    command.Parameters.AddWithValue("@Id", bookId);

                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }

            MessageBox.Show("Book updated successfully.");
           
        }

        private void LoadStatisticsData()
        {

            Labels = new List<string> { "Today", "Month", "Year" };

            // Users table
            int membersDay = GetCountFromQuery("SELECT COUNT(*) FROM Users WHERE strftime('%Y-%m-%d', JoinDate) = strftime('%Y-%m-%d', 'now')");
            int membersMonth = GetCountFromQuery("SELECT COUNT(*) FROM Users WHERE strftime('%Y-%m', JoinDate) = strftime('%Y-%m', 'now')");
            int membersYear = GetCountFromQuery("SELECT COUNT(*) FROM Users WHERE strftime('%Y', JoinDate) = strftime('%Y', 'now')");

            // Books table
            int booksDay = GetCountFromQuery("SELECT COUNT(*) FROM Books WHERE strftime('%Y-%m-%d', JoinDate) = strftime('%Y-%m-%d', 'now')");
            int booksMonth = GetCountFromQuery("SELECT COUNT(*) FROM Books WHERE strftime('%Y-%m', JoinDate) = strftime('%Y-%m', 'now')");
            int booksYear = GetCountFromQuery("SELECT COUNT(*) FROM Books WHERE strftime('%Y', JoinDate) = strftime('%Y', 'now')");

            // Reservations table
            int reservationsDay = GetCountFromQuery("SELECT COUNT(*) FROM Reservations WHERE strftime('%Y-%m-%d', ReservationDate) = strftime('%Y-%m-%d', 'now')");
            int reservationsMonth = GetCountFromQuery("SELECT COUNT(*) FROM Reservations WHERE strftime('%Y-%m', ReservationDate) = strftime('%Y-%m', 'now')");
            int reservationsYear = GetCountFromQuery("SELECT COUNT(*) FROM Reservations WHERE strftime('%Y', ReservationDate) = strftime('%Y', 'now')");


            NewMembersSeries = new SeriesCollection
{
    new ColumnSeries
    {
        Title = "New Members",
        Values = new ChartValues<int> { membersDay, membersMonth, membersYear }
    }
};

            NewBooksSeries = new SeriesCollection
{
    new ColumnSeries
    {
        Title = "New Books",
        Values = new ChartValues<int> { booksDay, booksMonth, booksYear }
    }
};

            BookReservationsSeries = new SeriesCollection
{
    new ColumnSeries
    {
        Title = "Reservations",
        Values = new ChartValues<int> { reservationsDay, reservationsMonth, reservationsYear }
    }
};
            Formatter = value => value.ToString("N0");

            DataContext = this;
        }

        private int GetCountFromQuery(string query)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    object result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Login_Page(_mainFrame));
        }

        private void LoadUsersFromDatabase()
        {
            UsersList.Clear();

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Id, Username, Password, Email, Phone, JoinDate FROM Users";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        UsersList.Add(new User
                        {
                            UserId = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Email = reader.GetString(3),
                            Phone = reader.GetString(4),
                            JoinDate = reader.GetString(5)
                        });
                    }
                }
            }

            MembersTable.ItemsSource = UsersList;
        }

        // Search user by username
        private void SearchUser_Click(object sender, RoutedEventArgs e)
        {
            string searchText = MemberSearchBox.Text.ToLower();
            var filtered = UsersList.Where(u => u.Username.ToLower().Contains(searchText)).ToList();
            MembersTable.ItemsSource = filtered;
        }

        // Reload all users
        private void LoadUsers_Click(object sender, RoutedEventArgs e)
        {
            LoadUsersFromDatabase();
        }

        public static void AddUserToDatabase(string username, string password, string email, string phone)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"SELECT 
                     (SELECT COUNT(*) FROM Users WHERE Username = @Username) AS UsernameCount,
                     (SELECT COUNT(*) FROM Users WHERE Email = @Email) AS EmailCount,
                     (SELECT COUNT(*) FROM Users WHERE Phone = @Phone) AS PhoneCount;
";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            List<string> duplicates = new List<string>();

                            if (reader.GetInt64(0) > 0)
                                duplicates.Add("Username");
                            if (reader.GetInt64(1) > 0)
                                duplicates.Add("Email");
                            if (reader.GetInt64(2) > 0)
                                duplicates.Add("Phone");

                            if (duplicates.Count > 0)
                            {
                                string message = string.Join(" and ", duplicates) + " already exist(s) in the database.";
                                MessageBox.Show(message);
                                return;
                            }
                        }
                    }
                }


                // Insert the new user
                query = "INSERT INTO Users (Username, Password, Email, Phone, JoinDate) VALUES (@Username, @Password, @Email, @Phone, @JoinDate)";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));

                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddMember_Click(object sender, RoutedEventArgs e)
        {
            String username = NewMemberName.Text.Trim();
            String email = NewMemberEmail.Text.Trim();
            String phone = NewMemberPhone.Text.Trim();
            String password = NewMemberPassword.Password;

            if (ValidateUserInput(password, email, phone)) 
            {
                AddUserToDatabase(username, password, email, phone);
            }
                
        }

        private bool IsPasswordValid(string password)
        {
            if (password.Length < 8)
                return false;

            if (!password.Any(char.IsUpper))
                return false;

            if (!password.Any(char.IsLower))
                return false;

            if (!password.Any(char.IsDigit))
                return false;

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                return false;

            return true;
        }


        private bool IsEmailValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // must contain @
            if (!email.Contains("@"))
                return false;

            // must contain .
            if (!email.Contains("."))
                return false;

            // very basic structure check
            int atIndex = email.IndexOf("@");
            int dotIndex = email.LastIndexOf(".");

            if (atIndex < 1 || dotIndex < atIndex + 2 || dotIndex == email.Length - 1)
                return false;

            return true;
        }

        private bool IsPhoneValid(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // allow "0xxxxxxxxx" (10 digits)
            if (phone.Length == 10 && phone.StartsWith("0") && phone.All(char.IsDigit))
                return true;

            // allow "+27xxxxxxxxx" (12 digits)
            if (phone.Length == 12 && phone.StartsWith("+27") && phone.Skip(3).All(char.IsDigit))
                return true;

            return false;
        }

        private bool ValidateUserInput(string password, string email, string phone)
        {
            if (!IsPasswordValid(password))
            {
                MessageBox.Show("Password must be at least 8 characters long, with uppercase, lowercase, number, and special character.");
                return false;
            }

            if (!IsEmailValid(email))
            {
                MessageBox.Show("Please enter a valid email address.");
                return false;
            }

            if (!IsPhoneValid(phone))
            {
                MessageBox.Show("Please enter a valid phone number (e.g. 0821234567 or +27821234567).");
                return false;
            }

            return true;
        }
    }
}

  
 public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string JoinDate { get; set; }
}

//thabo_m Thabo@2024!
