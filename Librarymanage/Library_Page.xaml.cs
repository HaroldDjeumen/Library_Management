using Newtonsoft.Json;
using System.Data.SQLite;
using System.IO;
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
        public BitmapImage ImageBitmap { get; set; }
        public string? PreviewUrl { get; set; }

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
        private string _currentBookTitle;
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
           
            Loaded += async (s, e) =>
{
    await PreviewBrowser.EnsureCoreWebView2Async(null);
    PreviewBrowser.Source = new Uri("https://www.google.com");
};

            
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
                await Dispatcher.InvokeAsync(() =>
                {
                    StackPanel bookPanel = new StackPanel
                    {
                        Width = 132,
                        Margin = new Thickness(2,5,5,5),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Width = 120,
                        Height = 190
                    };

                    if (book.ImageBitmap != null)
                    {
                        cover.Source = book.ImageBitmap;
                    }
                    else
                    {
                        // Use default image if book has no image
                        cover.Source = new BitmapImage(new Uri("Images/default-book.png", UriKind.Relative));
                    }

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

            await Task.WhenAll(tasks);
        }



        private List<Book> LoadBooksFromDatabase(string query = "")
        {
            List<Book> books = new List<Book>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string sql = string.IsNullOrEmpty(query)
                    ? "SELECT * FROM Books"
                    : "SELECT * FROM Books WHERE Name LIKE @query OR Author LIKE @query";

                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        command.Parameters.AddWithValue("@query", "%" + query + "%");
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] imageBytes = reader["BookImage"] as byte[];

                            BitmapImage bookImage = null;
                            if (imageBytes != null && imageBytes.Length > 0)
                            {
                                using (var stream = new MemoryStream(imageBytes))
                                {
                                    bookImage = new BitmapImage();
                                    bookImage.BeginInit();
                                    bookImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bookImage.StreamSource = stream;
                                    bookImage.EndInit();
                                }
                            }

                            books.Add(new Book
                            {
                                Title = reader["Name"].ToString(),
                                Author = reader["Author"].ToString(),
                                ReleaseDate = reader["Release-Date"].ToString(), 
                                ISBN = reader["ISBN"].ToString(),
                                Summary = reader["Description"].ToString(),
                                ImageBitmap = bookImage,
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

            BigCoverImage.Source = book.ImageBitmap;
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
                bookbutton.Visibility = Visibility.Collapsed;
                BookAvail.Text = "This book is currently reserved.";
                BookAvail.Visibility = Visibility.Visible;
            }
            else
            {
                bookbutton.Visibility = Visibility.Visible;
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
                bookbutton.Visibility = Visibility.Collapsed;
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





        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim();

            if (!string.IsNullOrEmpty(query))
            {
                BooksPanel.Children.Clear(); // Clear old books

                List<Book> searchResults = LoadBooksFromDatabase(query);

                if (searchResults.Count == 0)
                {
                    MessageBox.Show("No books found.");
                    return;
                }

                foreach (var book in searchResults)
                {

                    StackPanel bookPanel = new StackPanel
                    {
                        Width = 132,
                        Margin = new Thickness(2, 5, 5, 5),
                        Cursor = Cursors.Hand
                    };

                    Image cover = new Image
                    {
                        Source = book.ImageBitmap,
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

                    bookPanel.Children.Add(cover);
                    bookPanel.Children.Add(title);

                    bookPanel.MouseLeftButtonUp += (s2, e2) => ShowBookDetails(book);

                    BooksPanel.Children.Add(bookPanel);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new Login_Page(_mainFrame));
        }

        private void Accountdetail_clicked(object sender, RoutedEventArgs e)
        {
            ReservePanel.Visibility = Visibility.Collapsed;
            BooksPanel.Visibility = Visibility.Collapsed;
            PreviewPanel.Visibility = Visibility.Collapsed;
            AccountPanel.Visibility = Visibility.Visible; // <- Show Account Details
            LoadAccountDetails(); // <- Load user details
        }

        private void Library_clicked(object sender, RoutedEventArgs e)
        {
            LoadBooks();
            ReservePanel.Visibility = Visibility.Collapsed;
            AccountPanel.Visibility = Visibility.Collapsed;
            PreviewPanel.Visibility = Visibility.Collapsed;
            BooksPanel.Visibility = Visibility.Visible; // <- Show books panel
        }

        private void Reservation_clicked(object sender, RoutedEventArgs e)
        {
            BooksPanel.Visibility = Visibility.Collapsed;
            AccountPanel.Visibility = Visibility.Collapsed;
            PreviewPanel.Visibility = Visibility.Collapsed;
            ReservePanel.Visibility = Visibility.Visible; // <- Show reserve panel
            LoadReservedBooksFromDatabase(); // <- Load reserved books
        }


        private void LoadReservedBooksFromDatabase()
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
                                Text = $"Book: {bookTitle}\nReserved from: {reservationDate}\nTo: {returnDate}",
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

        private void EbookButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LoadAccountDetails()
        {
            AccountPanel.Children.Clear();

            // Example: Load account info from database
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Users WHERE Username = @Username";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", _currentUsername);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Example: Show account info
                            StackPanel accountInfo = new StackPanel { Margin = new Thickness(10) };

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = "Account Details",
                                FontSize = 20,
                                FontWeight = FontWeights.Bold,
                                Foreground = Brushes.Purple,
                                Margin = new Thickness(0, 0, 0, 10)
                            });

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = "Username: " + reader["Username"].ToString(),
                                FontSize = 16
                            });

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = "Email: " + reader["Email"].ToString(),
                                FontSize = 16
                            });

                            accountInfo.Children.Add(new TextBlock
                            {
                                Text = "Phone: " + reader["Phone"].ToString(),
                                FontSize = 16
                            });

                            AccountPanel.Children.Add(accountInfo);
                        }
                    }
                }
            }
        }

        private async void OpenPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedBook.Title))
            {
                MessageBox.Show("Error getting Book Title");
                return;
            }

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Name, PreviewUrl FROM Books WHERE Name LIKE @Title LIMIT 1";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", "%" + _selectedBook.Title + "%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string foundTitle = reader["Name"].ToString();
                            string previewUrl = reader["PreviewUrl"].ToString();

                            if (!string.IsNullOrEmpty(previewUrl) && previewUrl != "no preview available")
                            {
                                BookTitle.Text = foundTitle;

                                await PreviewBrowser.EnsureCoreWebView2Async(null);

                                if (PreviewBrowser.CoreWebView2 != null)
                                {
                                    PreviewBrowser.CoreWebView2.Navigate(previewUrl);
                                    PreviewPanel.Visibility = Visibility.Visible;
                                    ReservePanel.Visibility = Visibility.Collapsed;
                                    BooksPanel.Visibility = Visibility.Collapsed;
                                    AccountPanel.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    MessageBox.Show("CoreWebView2 failed ❌");
                                }

                                PreviewBrowser.CoreWebView2.NavigationCompleted += async (s, ev) =>
                                {
                                    await PreviewBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                                            document.body.style.backgroundColor = 'white';
                                            document.body.style.color = 'black';
                                            document.body.innerHTML = '<h1 style=""color:blue;"">Hello from WebView2</h1>';
                                    ");

                                };

                            }
                            else
                            {
                                MessageBox.Show("This book does not have a preview.");
                                PreviewPanel.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            MessageBox.Show("No book found with that title.");
                        }
                    }
                }
            }
        }


        private async void OpenEbookButton_Click(object sender, RoutedEventArgs e)
        {
            await PreviewBrowser.EnsureCoreWebView2Async(null);

            if (PreviewBrowser.CoreWebView2 != null)
            {
                PreviewBrowser.CoreWebView2.Navigate("https://openlibrary.org");
                PreviewPanel.Visibility = Visibility.Visible;
                ReservePanel.Visibility = Visibility.Collapsed;
                BooksPanel.Visibility = Visibility.Collapsed;
                AccountPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("CoreWebView2 failed ❌");
            }

            PreviewBrowser.CoreWebView2.NavigationCompleted += async (s, ev) =>
            {
                await PreviewBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                                            document.body.style.backgroundColor = 'white';
                                            document.body.style.color = 'black';
                                            document.body.innerHTML = '<h1 style=""color:blue;"">Hello from WebView2</h1>';
                                    ");

            };
        }

        private async void educationbooks_clicked(object sender, RoutedEventArgs e)
        {
            await PreviewBrowser.EnsureCoreWebView2Async(null);

            if (PreviewBrowser.CoreWebView2 != null)
            {
                PreviewBrowser.CoreWebView2.Navigate("https://openlibrary.org");
                PreviewPanel.Visibility = Visibility.Visible;
                ReservePanel.Visibility = Visibility.Collapsed;
                BooksPanel.Visibility = Visibility.Collapsed;
                AccountPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("CoreWebView2 failed ❌");
            }

            PreviewBrowser.CoreWebView2.NavigationCompleted += async (s, ev) =>
            {
                await PreviewBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                                            document.body.style.backgroundColor = 'white';
                                            document.body.style.color = 'black';
                                            document.body.innerHTML = '<h1 style=""color:blue;"">Hello from WebView2</h1>';
                                    ");

            };
        }

        private async void Readmangas_clicked(object sender, RoutedEventArgs e)
        {
            await PreviewBrowser.EnsureCoreWebView2Async(null);

            if (PreviewBrowser.CoreWebView2 != null)
            {
                PreviewBrowser.CoreWebView2.Navigate("https://mangadex.org");
                PreviewPanel.Visibility = Visibility.Visible;
                ReservePanel.Visibility = Visibility.Collapsed;
                BooksPanel.Visibility = Visibility.Collapsed;
                AccountPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("CoreWebView2 failed ❌");
            }

            PreviewBrowser.CoreWebView2.NavigationCompleted += async (s, ev) =>
            {
                await PreviewBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                                            document.body.style.backgroundColor = 'white';
                                            document.body.style.color = 'black';
                                            document.body.innerHTML = '<h1 style=""color:blue;"">Hello from WebView2</h1>';
                                    ");

            };
        }

        private async void Readebooks_clicked(object sender, RoutedEventArgs e)
        {
            await PreviewBrowser.EnsureCoreWebView2Async(null);

            if (PreviewBrowser.CoreWebView2 != null)
            {
                PreviewBrowser.CoreWebView2.Navigate("https://openlibrary.org");
                PreviewPanel.Visibility = Visibility.Visible;
                ReservePanel.Visibility = Visibility.Collapsed;
                BooksPanel.Visibility = Visibility.Collapsed;
                AccountPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("CoreWebView2 failed ❌");
            }

            PreviewBrowser.CoreWebView2.NavigationCompleted += async (s, ev) =>
            {
                await PreviewBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                                            document.body.style.backgroundColor = 'white';
                                            document.body.style.color = 'black';
                                            document.body.innerHTML = '<h1 style=""color:blue;"">Hello from WebView2</h1>';
                                    ");

            };
        }
    }
}


        


