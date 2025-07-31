# TechNova - ASP.NET MVC eCommerce Website

**TechNova** is a modern eCommerce web application built with ASP.NET MVC. It allows users to browse and purchase electronic products, manage their cart and wishlist, and register or log in to their account. The project includes a responsive UI inspired by real-world eCommerce platforms.

---

## 🚀 Features

- 🛍️ Product listing and detail pages
- 🛒 Add to Cart and Checkout functionality
- ❤️ Wishlist support
- 🔐 User authentication (Login/Register)
- 📞 Contact page and About page
- 🧾 Responsive UI using Bootstrap 5
- 📂 Structured layout with header and footer

---

## 🛠️ Tech Stack

- ASP.NET MVC (.NET Framework)
- SQL Server LocalDB
- Entity Framework
- Bootstrap 5
- jQuery
- Font Awesome (CDN)
- HTML/CSS

---

## 📁 Folder Structure

```
TechNova/
│
├── Controllers/
│   ├── HomeController.cs
│   ├── ProductController.cs
│   ├── CartController.cs
│   ├── AccountController.cs
│   └── WishlistController.cs
│
├── Models/
│   ├── Product.cs
│   ├── User.cs
│   ├── Category.cs
│   ├── CartItem.cs
│   ├── Order.cs
│   └── WishlistItem.cs
│
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml
│   │   └── About.cshtml
│   ├── Product/
│   │   └── Details.cshtml
│   ├── Cart/
│   │   ├── Index.cshtml
│   │   └── Checkout.cshtml
│   ├── Account/
│   │   ├── Login.cshtml
│   │   └── Register.cshtml
│   ├── Wishlist/
│   │   └── Index.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _ValidationScriptsPartial.cshtml
│
├── wwwroot/
│   ├── css/
│   │   └── site.css
│   ├── js/
│   │   └── site.js
│   └── images/
│       ├── logo.png
│       ├── google-play.png
│       └── app-store.png
│
├── appsettings.json
├── Startup.cs
└── README.md
```

---

## ⚙️ Configuration

1. Clone the repository:
   ```bash
   git clone https://github.com/kishankumar2607/TechNova.git
   cd TechNova
   ```

2. Set your database connection in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=NovaTechDatabase;Integrated Security=True"
   }
   ```

3. Use **Entity Framework Designer** to connect to your SQL Server database or run the migration manually.

---

## ▶️ Running the Project

1. Open the solution in **Visual Studio**
2. Set the startup project to `TechNova`
3. Press `F5` to build and run

---

## 📬 Contact

For any queries or support, contact:

**Kishan Kumar Das**  
📧 kishank2607@gmail.com  
🌐 [LinkedIn](https://www.linkedin.com/in/kishan-kumar-das/)  
