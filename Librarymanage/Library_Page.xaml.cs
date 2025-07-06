using Newtonsoft.Json;
using System.Data.SQLite;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Librarymanage
{
    // Ensure the 'Book' class is defined within the namespace or imported from another namespace  
    public class Book
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string ReleaseDate { get; set; }
        public string ISBN { get; set; }
        public string ImagePath { get; set; }
        public string Summary { get; set; }
    }

   

    public class Reservation
    {
        public string Username { get; set; }
        public string BookTitle { get; set; }
        public DateTime ReservationDate { get; set; }
    }

    public partial class Library_Page : Page
    {
        private Frame _mainFrame;
        private string _currentUsername;
        private Book _selectedBook;
        private static string connectionString = "Data Source=C:\\Users\\darre\\OneDrive\\Documents\\files\\Librarymanage\\Data\\Library.db;Version=3;";
        public Library_Page(Frame mainFrame, string username)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            _currentUsername = username;

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

            LoadBooks();
            

            // Fix: Pass a valid book title to ReserveBook
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
                        Width = 150,
                        Margin = new Thickness(10),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Source = new BitmapImage(new Uri(book.ImagePath, UriKind.RelativeOrAbsolute)),
                        Width = 150,
                        Height = 200
                    };

                    TextBlock title = new TextBlock
                    {
                        Text = book.Title,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Width = 140
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
            ReservationCalendar.Visibility = Visibility.Collapsed;
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
            ReservationCalendar.Visibility = Visibility.Visible;
            ReturnCalendar.Visibility = Visibility.Visible;

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
                ReservationCalendar.Visibility = Visibility.Collapsed;
                ReturnCalendar.Visibility = Visibility.Collapsed;
                OpenCalendarButton.Visibility = Visibility.Collapsed;
                ReservationPolicyText.Visibility = Visibility.Collapsed;
                BookAvail.Text = "Status: Reserved";
                BookAvail.Visibility = Visibility.Visible;

                MessageBox.Show($"Book '{_selectedBook.Title}' reserved by {_currentUsername} from {reservationDate:dd/MM/yyyy} to {returnDate:dd/MM/yyyy}.");
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
                        Width = 150,
                        Margin = new Thickness(10),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute)),
                        Width = 150,
                        Height = 200
                    };

                    TextBlock title = new TextBlock
                    {
                        Text = book.Title,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Width = 140
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
    }
}
