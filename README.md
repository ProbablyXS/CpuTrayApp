# CPU Power Manager (CpuTrayApp)

<table align="center">
    <td>
      <img src="https://github.com/user-attachments/assets/ee7386c1-00a3-4356-964d-9c9b8380afe5" width="60" height="60" alt="favicon" />
    </td>
</table>

**CPU Power Manager** is a handy Windows application that sits in the system tray to easily manage **power plans** and **CPU limits**. It also allows you to monitor the processor’s current clock speed in real-time.

---

## Table of Contents
- [Features](#features)
- [Screenshot](#screenshot)
- [Installation](#installation)
- [Usage](#usage)
- [Technologies](#technologies)
- [Contributing](#contributing)
- [License](#license)
- [Author](#author)

---

## Features

- ✅ List and select **available power plans** on your PC.
- ✅ Set **CPU limit** by percentage (10% to 100%).
- ✅ Display the **current CPU frequency** in real-time.
- ✅ Automatically clean the process memory to optimize performance.
- ✅ System tray icon with easy-to-use context menus.
- ✅ Dynamic updates when hovering over the icon or opening the menu.

---

## Screenshot 

## <img width="302" height="92" alt="{09DBC937-7AEE-4E61-86AE-471B23BE11D2}" src="https://github.com/user-attachments/assets/3f90acc1-ad8b-4119-a55b-cd1df3771d8a" />

## <img width="479" height="235" alt="{50B64EE2-9FFF-4E1F-B106-DAAE6F36AEC5}" src="https://github.com/user-attachments/assets/0f9d8a4a-559e-4e9d-9cb0-21e5c003a972" />

---

## Installation

1. **Prerequisites:**  
   - Windows 10 or higher  
   - .NET Framework 4.7.2 or higher (or .NET 6/7 depending on compilation)  

2. **Download:**  
   - Clone the repository:  
     ```bash
     git clone https://github.com/your-username/CpuTrayApp.git
     ```
   - Open the project in Visual Studio.
   - Build in `Release` mode to get the executable.

3. **Run:**  
   - Launch `CpuTrayApp.exe`.
   - The application will automatically appear in the **system tray**.

---

## Usage

1. Click the icon in the system tray.
2. Select a **power plan**.
3. Choose the **CPU limit** from the “CPU limit (%)” menu.
4. Hover over the icon to update the CPU frequency.
5. Exit via the “Exit” menu.

---

## Technologies

- **C# / .NET**  
- **WinForms** for system tray interface and context menus.
- **ManagementObjectSearcher** to retrieve CPU frequency.
- **PowerCfg** (Windows command) to manage power plans.
- **psapi.dll** to free process memory.

---

## Contributing

Contributions are welcome!  
You can:  
- Open **issues** to report bugs or suggest improvements.
- Submit **pull requests** to add new features.
- Share ideas to improve the application.

---

## License

This project is licensed under the **MIT License**.  
See the [LICENSE](LICENSE) file for details.

---

## Author

- **Name**: [Your Name]  
- **GitHub**: [https://github.com/your-username](https://github.com/ProbablyXS)  
