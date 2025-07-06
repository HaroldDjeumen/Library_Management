using Newtonsoft.Json;
using System.Data.SQLite;
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
        private static string connectionString = "Data Source=C:\\Users\\hpie9\\Documents\\Librarymanage\\Librarymanage\\Data\\Library.db;Version=3;";

        public Admin_Page(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;

            // Make the parent window full screen
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.WindowState = WindowState.Maximized;
                
            }
            else
            {
                this.Loaded += (s, e) =>
                {
                    var win = Window.GetWindow(this);
                    if (win != null)
                    {
                        win.WindowState = WindowState.Maximized;
                        
                    }
                };
            }
        }

        private void BooksButton_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            BookAdminPanel.Visibility = Visibility.Visible;
            ContentPanel.Children.Add(BookAdminPanel);
            LoadBooksFromDatabase();
        }

        private void EventsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadContent("Events Management");
        }

        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            LoadContent("Members Management");
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadContent("Statistics Overview");
        }

        private void LoadContent(string contentTitle)
        {
            ContentPanel.Children.Clear(); // Clear previous content

            // Example: Add a title to the panel
            TextBlock title = new TextBlock
            {
                Text = contentTitle,
                FontSize = 32,
                Foreground = System.Windows.Media.Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            ContentPanel.Children.Add(title);
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
                TextBlock description = new TextBlock
                {
                    Text = book.Summary,
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 100,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                Button addButton = new Button
                {
                    Content = "Add to Database",
                    Margin = new Thickness(0, 10, 0, 0),
                    Tag = book
                };
                addButton.Click += AddBookToDatabase;
                

                // Build details panel
                detailsPanel.Children.Add(title);
                detailsPanel.Children.Add(author);
                detailsPanel.Children.Add(release);
                detailsPanel.Children.Add(description);
                detailsPanel.Children.Add(addButton);

                // Build book panel
                bookPanel.Children.Add(coverImage);
                bookPanel.Children.Add(detailsPanel);

                bookBorder.Child = bookPanel;

                ApiBooksPanel.Children.Add(bookBorder);
            }
        }


        // Fix for CS1002 and CS0029 errors in the AddBookToDatabase method
        private void AddBookToDatabase(object sender, RoutedEventArgs e)
        {
            Button addButton = sender as Button;
            Book selectedBook = (Book)addButton.Tag;

            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string insertQuery = "INSERT INTO Books (Name, [Release-Date], Author, ISBN, Description) VALUES (@Name, @ReleaseDate, @Author, @ISBN, @Description)";

                using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                {
                    // Fix the release date format to allow YYYY-MM-DD and YYYY
                    DateTime releaseDate;
                    if (DateTime.TryParseExact(selectedBook.ReleaseDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out releaseDate) ||
                        DateTime.TryParseExact(selectedBook.ReleaseDate, "yyyy", null, System.Globalization.DateTimeStyles.None, out releaseDate))
                    {
                        cmd.Parameters.AddWithValue("@ReleaseDate", releaseDate);
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

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show($"Book '{selectedBook.Title}' added successfully!");
                        LoadBooksFromDatabase(); // Refresh the book list
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding book: {ex.Message}");
                    }
                }
            }
        }

        private async Task<List<Book>> SearchBooksFromAPI(string searchQuery)
        {
            List<Book> books = new List<Book>();
            List<string> queries = new List<string>
    {
        searchQuery,
        "Science Fiction Novel",
        "Wimpy Kid",
        "Kids Books",
        "Romance Novels",
        "Mystery Novels",

    };

            using (HttpClient client = new HttpClient())
            {
                foreach (var query in queries)
                {
                    try
                    {
                        string Url = $"https://www.googleapis.com/books/v1/volumes?q={query.Replace(" ", "+")}&maxResults=40";
                        var response = await client.GetAsync(Url);
                        var jsonString = await response.Content.ReadAsStringAsync();

                        dynamic data = JsonConvert.DeserializeObject(jsonString);

                        if (data.items != null && data.items.Count > 0)
                        {
                            foreach (var item in data.items)
                            {
                                var bookInfo = item.volumeInfo;


                                if (bookInfo.imageLinks == null || string.IsNullOrEmpty((string)bookInfo.imageLinks.thumbnail))
                                    continue;

                                string imagePath = ((string)bookInfo.imageLinks.thumbnail).Replace("http://", "https://");
                                if (imagePath == "https://via.placeholder.com/150")
                                    continue;


                                if (bookInfo.authors == null || bookInfo.authors.Count == 0)
                                    continue;

                                string[] authorsArray = bookInfo.authors.ToObject<string[]>();
                                if (authorsArray.Length == 0 || authorsArray[0].ToLower() == "unknown")
                                    continue;


                                if (bookInfo.description == null || string.IsNullOrEmpty((string)bookInfo.description) || ((string)bookInfo.description).ToLower() == "unknown")
                                    continue;


                                if (bookInfo.industryIdentifiers == null || bookInfo.industryIdentifiers.Count == 0)
                                    continue;

                                books.Add(new Book
                                {
                                    Title = bookInfo.title,
                                    Author = string.Join(", ", authorsArray),
                                    ReleaseDate = bookInfo.publishedDate != null ? bookInfo.publishedDate.ToString() : "Unknown",
                                    ISBN = bookInfo.industryIdentifiers[0].identifier.ToString(),
                                    ImagePath = imagePath,
                                    Summary = bookInfo.description.ToString()
                                });

                                if (books.Count >= 200) break; //  Load 200 books now
                            }
                        }

                        if (books.Count >= 200) break; //  Stop searching if enough books are found
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error fetching data from API: {ex.Message}");
                    }
                }
            }

            return books;
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
                        StackPanel bookRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

                        // Show book name and author
                        TextBlock bookInfo = new TextBlock
                        {
                            Text = $"{reader["Name"]} by {reader["Author"]}",
                            Width = 600,
                            FontSize = 16
                        };

                        // Delete button
                        Button deleteButton = new Button
                        {
                            Content = "Delete",
                            Margin = new Thickness(5),
                            Background = Brushes.Red,
                            Foreground = Brushes.White
                        };
                        int bookId = Convert.ToInt32(reader["Id"]);
                        deleteButton.Click += (s, e) => DeleteBook(bookId);

                        // Modify button
                        Button modifyButton = new Button
                        {
                            Content = "Modify",
                            Margin = new Thickness(5),
                            Background = Brushes.Orange,
                            Foreground = Brushes.White
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

            MessageBox.Show("Book deleted.");
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
                            string name = reader["Name"].ToString();
                            string author = reader["Author"].ToString();
                            string releaseDate = reader["Release-Date"].ToString();
                            string isbn = reader["ISBN"].ToString();
                            string description = reader["Description"].ToString();
                            

                            // Show an input form
                            ShowModifyForm(bookId, name, author, releaseDate, isbn, description);
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
                }
            }

            MessageBox.Show("Book updated successfully.");
            LoadBooksFromDatabase(); // Refresh the list
        }

        
    }
}
