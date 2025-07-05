using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

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

        private async Task LoadBooks(string searchQuery = "Harry Potter")
        {
            List<Book> books = await SearchBooksFromAPI(searchQuery);

            foreach (var book in books)
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
            }
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

            // Limit the calendar selection
            ReservationCalendar.DisplayDateStart = DateTime.Now;
            ReservationCalendar.DisplayDateEnd = DateTime.Now.AddMonths(2);
        }

        private void ReservationCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReservationCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = ReservationCalendar.SelectedDate.Value;
                SelectedReservationDateText.Text = "Reserved Until: " + selectedDate.ToString("dd/MM/yyyy");

                // Show policies and confirm button
                ReservationPolicyText.Visibility = Visibility.Visible;
                ConfirmReservationButton.Visibility = Visibility.Visible;
            }
        }

        private void ConfirmReservationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBook != null && ReservationCalendar.SelectedDate.HasValue)
            {
                string reservationDate = ReservationCalendar.SelectedDate.Value.ToString("dd/MM/yyyy");

                // Save to reservations.txt
                string reservationInfo = $"{_currentUsername};{_selectedBook.Title};{reservationDate}";
                File.AppendAllText("reservations.txt", reservationInfo + Environment.NewLine);

                ConfirmReservationButton.Visibility = Visibility.Collapsed;
                ReservationCalendar.Visibility = Visibility.Collapsed;
                OpenCalendarButton.Visibility = Visibility.Collapsed;
                ReservationPolicyText.Visibility = Visibility.Collapsed;
                BookAvail.Text = "Status: Reserved";
                BookAvail.Visibility = Visibility.Visible;

                MessageBox.Show($"Book '{_selectedBook.Title}' has been reserved by {_currentUsername} until {reservationDate}.");
            }
        }

        private bool IsBookReserved(string bookTitle)
        {
            if (!File.Exists("Reservations.txt"))
                return false;

            var reservations = File.ReadAllLines("Reservations.txt");

            foreach (var res in reservations)
            {
                var parts = res.Split(';');
                if (parts.Length >= 3 && parts[1].Trim().Equals(bookTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    DateTime reservedUntil = DateTime.ParseExact(parts[2], "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    if (reservedUntil >= DateTime.Now)
                    {
                        return true;
                    }
                }
            }
            return false;
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


        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                BooksPanel.Children.Clear(); // Clear old books
                await LoadBooks(query);
            }
        }


    }
}
