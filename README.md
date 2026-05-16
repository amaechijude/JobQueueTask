# JobQueueTask

A robust, asynchronous background job queuing and processing API built with ASP.NET Core. It uses **PostgreSQL** for persistent job storage and **Redis** for high-performance job queuing.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (8.0 or later)
- **PostgreSQL** (Installed locally or via Docker)
- **Redis** (Installed locally or via Docker)
- Docker (Optional, but recommended for easily running dependencies)

## Clone the Repository

To get started, clone the repository to your local machine using the following command:

```bash
git clone https://github.com/amaechijude/JobQueueTask.git
```

## Environment Setup

You can run PostgreSQL, Redis, and the API either directly on your machine or by using Docker. 

### Option A: Using Docker (Recommended)

If you have Docker installed, you can quickly spin up the entire application stack or just the dependencies using Docker Compose.

**1. Start the API, PostgreSQL, and Redis together:**
```bash
docker-compose up -d --build
```

### Option B: Local Installation

- **PostgreSQL**: Download and install from the [official website](https://www.postgresql.org/download/). Create a database named `jobqueue`.
- **Redis**: Download and install via Redis.io or use Memurai if you are on Windows natively.

## Configuration

Ensure your connection strings are correctly configured for your environment. Update your `appsettings.Development.json` (or `appsettings.json`) located in the `JobQueueTask.Api` folder:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=jobqueue;Username=postgres;Password=yourpassword",
    "redis": "localhost:6379"
  }
}
```

## Running the Application

1. Open a terminal and navigate to the project directory:
   ```bash
   cd JobQueueTask.Api
   ```
2. Build and run the project:
   ```bash
   dotnet run
   ```

*Note: When running in the `Development` environment, the application will automatically apply Entity Framework Core migrations to provision your PostgreSQL database on startup.*

## API Documentation

Once the application is up and running, you can explore and test the endpoints using the built-in Scalar API reference:

- Open your browser and navigate to: `https://localhost:<port>/scalar/v1` (replace `<port>` with the actual port shown in your terminal output).