﻿using Newtonsoft.Json;
using System.Data.SQLite;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Librarymanage
{
    // Ensure the 'Book' class is defined within the namespace or imported from another namespace  
    public class Book
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? ReleaseDate { get; set; }
        public string? ISBN { get; set; }
        public string? ImagePath { get; set; }
        public string? Summary { get; set; }
    }

   

    public class Reservation
    {
        public int ReservationId { get; set; }
        public string? Username { get; set; }
        public string? BookTitle { get; set; }
        public string? ReservationDate { get; set; }
        public string? ReturnDate { get; set; }
    }

    public partial class Library_Page : Page
    {
        private Frame _mainFrame;
        private string _currentUsername;
        private Book _selectedBook;
        private Reservation _selectedReservation;
        private static string connectionString = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Library.db")};Version=3;";



#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Library_Page(Frame mainFrame, string username)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            _currentUsername = username;
            AccountName.Text = username;

            LoadBooks();
            BooksPanel.Visibility = Visibility.Visible;
           
            
            if (_selectedBook != null)
            {
                ReserveBook(_selectedBook.Title);
            }
        }

        private async Task LoadBooks(string searchQuery = "")
        {
            BooksPanel.Children.Clear();

            List<Book> books = LoadBooksFromDatabase(searchQuery);

            var tasks = books.Select(async book =>
            {
                // Get image from API
                string imageUrl = await GetBookImageFromAPI(book.Title);

                // Set image to the book object
                book.ImagePath = imageUrl;

                // UI updates must be on the UI thread
                Dispatcher.Invoke(() =>
                {
                    StackPanel bookPanel = new StackPanel
                    {
                        Width = 141,
                        Margin = new Thickness(0,5,2,0),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Source = new BitmapImage(new Uri(book.ImagePath, UriKind.RelativeOrAbsolute)),
                        Width = 120,
                        Height = 190
                    };

                    TextBlock title = new TextBlock
                    {
                        Text = book.Title,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        Width = 120
                    };

                    bookPanel.Children.Add(cover);
                    bookPanel.Children.Add(title);
                    bookPanel.MouseLeftButtonUp += (s, e) => ShowBookDetails(book);

                    BooksPanel.Children.Add(bookPanel);
                });
            });

            // Wait for all images to load in parallel
            await Task.WhenAll(tasks);
        }

        private List<Book> LoadBooksFromDatabase(string searchQuery = "")
        {
            List<Book> books = new List<Book>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Books";

                // If the user typed something in search box, search for it
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query += " WHERE Name LIKE @SearchQuery";
                }

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                Title = reader["Name"].ToString(),
                                Author = reader["Author"].ToString(),
                                ReleaseDate = reader["Release-Date"].ToString(),
                                ISBN = reader["ISBN"].ToString(),
                                Summary = reader["Description"].ToString()
                                // The image will be loaded later from the API
                            });
                        }
                    }
                }
            }

            return books;
        }


        private void ShowBookDetails(Book book)
        {
            _selectedBook = book;

            BookDetailsPanel.Visibility = Visibility.Visible;

            BigCoverImage.Source = new BitmapImage(new Uri(book.ImagePath, UriKind.Absolute));
            BookTitle.Text = book.Title;
            BookAuthor.Text = "Author: " + book.Author;
            BookReleaseDate.Text = "Released: " + book.ReleaseDate;
            BookISBN.Text = "ISBN: " + book.ISBN;
            BookSummary.Text = book.Summary;

            // Reset
            CalenderView.Visibility = Visibility.Collapsed;
            SelectedReservationDateText.Text = "";
            ReservationPolicyText.Visibility = Visibility.Collapsed;
            ConfirmReservationButton.Visibility = Visibility.Collapsed;

            // Check if book is already reserved
            ReserveBook(book.Title);
        }

        private void ReserveBook(string bookTitle)
        {
            if (IsBookReserved(bookTitle))
            {
                OpenCalendarButton.Visibility = Visibility.Collapsed;
                BookAvail.Text = "This book is currently reserved.";
                BookAvail.Visibility = Visibility.Visible;
            }
            else
            {
                OpenCalendarButton.Visibility = Visibility.Visible;
                BookAvail.Visibility = Visibility.Collapsed;
            }
        }



        private void OpenCalendarButton_Click(object sender, RoutedEventArgs e)
        {
            CalenderView.Visibility = Visibility.Visible;

            ReservationPolicyText.Visibility = Visibility.Visible;
            

            // Limit the selection dates
            ReservationCalendar.DisplayDateStart = DateTime.Now;
            ReservationCalendar.DisplayDateEnd = DateTime.Now.AddMonths(1);

            ReturnCalendar.DisplayDateStart = DateTime.Now.AddDays(1);
            ReturnCalendar.DisplayDateEnd = DateTime.Now.AddMonths(2);
        }

        private void ReservationCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReservationInfo();
        }

        private void ReturnCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReservationInfo();
        }

        private void UpdateReservationInfo()
        {
            if (ReservationCalendar.SelectedDate.HasValue && ReturnCalendar.SelectedDate.HasValue)
            {
                DateTime reservationDate = ReservationCalendar.SelectedDate.Value;
                DateTime returnDate = ReturnCalendar.SelectedDate.Value;

                if (reservationDate >= returnDate)
                {
                    MessageBox.Show("Return date must be after reservation date.");
                    return;
                }

                SelectedReservationDateText.Text = $"Reserved: {reservationDate:dd/MM/yyyy} | Return: {returnDate:dd/MM/yyyy}";
                ReservationPolicyText.Visibility = Visibility.Visible;
                ConfirmReservationButton.Visibility = Visibility.Visible;
            }
        }


        private void ConfirmReservationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBook != null && ReservationCalendar.SelectedDate.HasValue && ReturnCalendar.SelectedDate.HasValue)
            {
                DateTime reservationDate = ReservationCalendar.SelectedDate.Value;
                DateTime returnDate = ReturnCalendar.SelectedDate.Value;

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO Reservations (Username, BookTitle, ReservationDate, ReturnDate) VALUES (@Username, @BookTitle, @ReservationDate, @ReturnDate)";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", _currentUsername);
                        command.Parameters.AddWithValue("@BookTitle", _selectedBook.Title);
                        command.Parameters.AddWithValue("@ReservationDate", reservationDate.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@ReturnDate", returnDate.ToString("yyyy-MM-dd"));

                        command.ExecuteNonQuery();
                    }
                }

                ConfirmReservationButton.Visibility = Visibility.Collapsed;
                CalenderView.Visibility = Visibility.Collapsed;
                OpenCalendarButton.Visibility = Visibility.Collapsed;
                ReservationPolicyText.Visibility = Visibility.Collapsed;
                BookAvail.Text = "This book is currently reserved";
                BookAvail.Visibility = Visibility.Visible;

               // MessageBox.Show($"Book '{_selectedBook.Title}' reserved by {_currentUsername} from {reservationDate:dd/MM/yyyy} to {returnDate:dd/MM/yyyy}.");
            }
        }


        private bool IsBookReserved(string bookTitle)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Reservations WHERE BookTitle = @BookTitle";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BookTitle", bookTitle);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime returnDate = DateTime.Parse(reader["ReturnDate"].ToString());
                            if (returnDate >= DateTime.Now)
                            {
                                return true; // Book is still reserved
                            }
                        }
                    }
                }
            }

            return false; // Book is available
        }





        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim();

            if (!string.IsNullOrEmpty(query))
            {
                BooksPanel.Children.Clear(); // Clear old books

                List<Book> searchResults = SearchBooksInDatabase(query);

                if (searchResults.Count == 0)
                {
                    MessageBox.Show("No books found.");
                    return;
                }

                foreach (var book in searchResults)
                {
                    // Get image from API
                    string imagePath = await GetBookImageFromAPI(book.Title);

                    StackPanel bookPanel = new StackPanel
                    {
                        Width = 141,
                        Margin = new Thickness(0,5,2,0),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute)),
                        Width = 120,
                        Height = 190
                    };

                    TextBlock title = new TextBlock
                    {
                        Text = book.Title,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Width = 120
                    };

                    book.ImagePath = imagePath;

                    bookPanel.Children.Add(cover);
                    bookPanel.Children.Add(title);

                    bookPanel.MouseLeftButtonUp += (s2, e2) => ShowBookDetails(book);

                    BooksPanel.Children.Add(bookPanel);
                }
            }
        }

        private List<Book> SearchBooksInDatabase(string searchQuery)
        {
            List<Book> books = new List<Book>();
            

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Books WHERE Name LIKE @SearchQuery";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                Title = reader["Name"].ToString(),
                                Author = reader["Author"].ToString(),
                                ReleaseDate = reader["Release-Date"].ToString(),
                                ISBN = reader["ISBN"].ToString(),
                                Summary = reader["Description"].ToString()
                                // Image will come from API
                            });
                        }
                    }
                }
            }

            return books;
        }



        private List<Book> GetBooksFromDatabase()
        {
            List<Book> books = new List<Book>();
            

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Books";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        books.Add(new Book
                        {
                            Title = reader["Name"].ToString(),
                            Author = reader["Author"].ToString(),
                            ReleaseDate = reader["Release-Date"].ToString(),
                            ISBN = reader["ISBN"].ToString(),
                            Summary = reader["Description"].ToString()
                            // No Image in DB, we will get it from API
                        });
                    }
                }
            }
            return books;
        }

        private async Task<string> GetBookImageFromAPI(string bookTitle)
        {
            string imageUrl = "https://via.placeholder.com/150"; // Default placeholder

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"https://www.googleapis.com/books/v1/volumes?q={bookTitle.Replace(" ", "+")}&maxResults=1";
                    var response = await client.GetAsync(url);
                    var jsonString = await response.Content.ReadAsStringAsync();

                    dynamic data = JsonConvert.DeserializeObject(jsonString);

                    if (data.items != null && data.items.Count > 0)
                    {
                        var bookInfo = data.items[0].volumeInfo;

                        if (bookInfo.imageLinks != null && bookInfo.imageLinks.thumbnail != null)
                        {
                            imageUrl = bookInfo.imageLinks.thumbnail.ToString().Replace("http://", "https://");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fetching image: {ex.Message}");
                }
            }

            return imageUrl;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Login_Page(_mainFrame));
        }

        private void Accountdetail_clicked(object sender, RoutedEventArgs e)
        {
            ReservePanel.Visibility = Visibility.Collapsed;
            BooksPanel.Visibility = Visibility.Collapsed;
        }

        private void Library_clicked(object sender, RoutedEventArgs e)
        {
            LoadBooks();
            ReservePanel.Visibility = Visibility.Collapsed;
            BooksPanel.Visibility = Visibility.Visible; // <- Show books panel
        }

        private void Reservation_clicked(object sender, RoutedEventArgs e)
        {
            BooksPanel.Visibility = Visibility.Collapsed;
            ReservePanel.Visibility = Visibility.Visible; // <- Show reserve panel
            LoadBooksFromDatabase(); // <- Load reserved books
        }


        private void LoadBooksFromDatabase()
        {
           // ReservePanel.Children.Clear(); // Clear old items

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Reservations WHERE Username = @Username";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", _currentUsername);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int reservationId = Convert.ToInt32(reader["Id"]);
                            string bookTitle = reader["BookTitle"].ToString();
                            string reservationDate = reader["ReservationDate"].ToString();
                            string returnDate = reader["ReturnDate"].ToString();

                            // Create card border
                            Border card = new Border
                            {
                                Background = Brushes.White,
                                BorderBrush = Brushes.Gray,
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(5),
                                Margin = new Thickness(5),
                                Padding = new Thickness(10),
                                Width = 276
                            };

                            StackPanel content = new StackPanel();

                            TextBlock bookInfo = new TextBlock
                            {
                                Text = $"Book: {bookTitle}\nReserved from: {reservationDate} To: {returnDate}",
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 14,
                                Margin = new Thickness(0, 0, 0, 5)
                            };

                            // Modify button
                            Button modifyButton = new Button
                            {
                                Content = "Edit Dates",
                                Margin = new Thickness(0, 5, 0, 5),
                                Background = Brushes.Orange,
                                Foreground = Brushes.White
                            };
                            modifyButton.Click += (s, e) =>
                            {
                                ModifyReservationDates(reservationId, reservationDate, returnDate);
                                LoadBooksFromDatabase(); // Refresh after edit
                            };

                            // Delete button
                            Button deleteButton = new Button
                            {
                                Content = "Delete",
                                Margin = new Thickness(0, 5, 0, 5),
                                Background = Brushes.Red,
                                Foreground = Brushes.White
                            };
                            deleteButton.Click += (s, e) =>
                            {
                                DeleteBook(reservationId);
                                LoadBooksFromDatabase(); // Refresh after delete
                            };

                            content.Children.Add(bookInfo);
                            content.Children.Add(modifyButton);
                            content.Children.Add(deleteButton);

                            card.Child = content;

                            ReservePanel.Children.Add(card);
                        }
                    }
                }
            }
        }



        private void DeleteBook(int reservationId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Reservations WHERE Id = @Id";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", reservationId);
                    command.ExecuteNonQuery();
                }
            }

            // MessageBox.Show("Book deleted.");
            LoadBooksFromDatabase(); // Refresh list
        }


        private void ModifyReservationDates(int reservationId, string reservationDate, string returnDate)
        {
            Window dateWindow = new Window
            {
                Title = "Edit Reservation Dates",
                Width = 300,
                Height = 250,
                Background = Brushes.White,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBox reservationBox = new TextBox { Text = reservationDate, Margin = new Thickness(0, 10, 0, 10) };
            TextBox returnBox = new TextBox { Text = returnDate, Margin = new Thickness(0, 0, 0, 10) };

            Button saveButton = new Button
            {
                Content = "Save",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                Width = 100,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            saveButton.Click += (s, e) =>
            {
                UpdateDatesOnly(reservationId, reservationBox.Text, returnBox.Text);
                dateWindow.Close();
            };

            panel.Children.Add(new TextBlock { Text = "Reservation Date:" });
            panel.Children.Add(reservationBox);
            panel.Children.Add(new TextBlock { Text = "Return Date:" });
            panel.Children.Add(returnBox);
            panel.Children.Add(saveButton);

            dateWindow.Content = panel;
            dateWindow.ShowDialog();
        }


        private void UpdateDatesOnly(int reservationId, string reservationDate, string returnDate)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"UPDATE Reservations 
                         SET ReservationDate = @ReservationDate, ReturnDate = @ReturnDate 
                         WHERE Id = @Id";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReservationDate", reservationDate);
                    command.Parameters.AddWithValue("@ReturnDate", returnDate);
                    command.Parameters.AddWithValue("@Id", reservationId);

                    command.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Reservation updated.");
        }

       
    }
}
