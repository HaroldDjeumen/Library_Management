﻿using Newtonsoft.Json;
using System.Data.SQLite;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Collections.Generic;

namespace Librarymanage
{
    /// <summary>
    /// Interaction logic for Admin_Page.xaml
    /// </summary>
    public partial class Admin_Page : Page
    {
        private Frame _mainFrame;
        private static string connectionString = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Library.db")};Version=3;";

        public Func<double, string> Formatter { get; set; }
        public SeriesCollection NewMembersSeries { get; set; }
        public SeriesCollection NewBooksSeries { get; set; }
        public SeriesCollection BookReservationsSeries { get; set; }
        public List<string> Labels { get; set; }



#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Admin_Page(Frame mainFrame)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            InitializeComponent();
            _mainFrame = mainFrame;

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

        private void EventsButton_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
          
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            BookAdminPanel.Visibility = Visibility.Collapsed;
            StatAdminPanel.Visibility = Visibility.Visible;
            if (StatAdminPanel.Parent is Panel parentPanel)
        {
             parentPanel.Children.Remove(StatAdminPanel);
        }

            ContentPanel.Children.Add(StatAdminPanel);
            LoadStatisticsData();
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



        private async void AddBookToDatabase(object sender, RoutedEventArgs e)
        {
            Button? addButton = sender as Button;
#pragma warning disable CS8602
            Book selectedBook = (Book)addButton.Tag;
#pragma warning restore CS8602

            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string insertQuery = @"INSERT INTO Books 
            (Name, [Release-Date], Author, ISBN, Description, JoinDate, BookImage) 
            VALUES 
            (@Name, @ReleaseDate, @Author, @ISBN, @Description, @JoinDate, @BookImage)";

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
            List<Book> books = new List<Book>();
            List<string> queries = new List<string>
    {
        searchQuery,

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

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        dynamic data = JsonConvert.DeserializeObject(jsonString);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
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

                                if (books.Count >= 40) break; //  Load 40 books now
                            }
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                        if (books.Count >= 40) break; //  Stop searching if enough books are found
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
                            Text = $"{reader["Name"]} by {reader["Author"]}, added on {reader["JoinDate"]}",
                            Width = 592,
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
                            Margin = new Thickness(5),
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
    }
}
