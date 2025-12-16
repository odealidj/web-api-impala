# Impala API

.NET 10 Minimal API dengan Vertical Slice Architecture untuk koneksi ke Apache Impala database via ODBC.

## ✨ Features

- ✅ .NET 10 Minimal API
- ✅ Vertical Slice Architecture
- ✅ Impala ODBC Integration (Cloudera Driver)
- ✅ Generic Repository Pattern + Dapper
- ✅ Health Checks & Graceful Shutdown
- ✅ Error Handling (503/500)
- ✅ Structured Logging (Serilog)
- ✅ OpenAPI/Swagger Documentation

## 🔌 API Endpoints

### Health Check
```http
GET /health
```

### Get Tables
```http
GET /api/tables
```

### Test Graceful Shutdown
```http
GET /api/slow?delaySeconds=8
```

## 📦 Tech Stack

- .NET 10
- System.Data.Odbc 10.0.1
- Dapper 2.1.66
- Serilog.AspNetCore 10.0.0
- Swashbuckle.AspNetCore 10.0.1

