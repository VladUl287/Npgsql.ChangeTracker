## 📋 TODO

### 📚 **Documentation**
- [ ] Write comprehensive API documentation
- [ ] Add usage examples and code samples
- [ ] Update README with advanced usage

### 🔧 **Core Features**
- [ ] **Add track attribute full options support through constructor**
  - [ ] Design new attribute configuration interface
  - [ ] Add validation logic for all services 
  - [ ] Make possible to register source provider not as singleton

### ⚡ **Optimization**
- [ ] Profile current performance bottlenecks
- [ ] Implement connection pooling improvements
- [ ] Add query result caching layer
- [ ] Optimize memory usage in bulk operations

### 🐘 **Npgsql Improvements**
- [ ] **"GetLastVersions" make more effective array parsing**
  - [ ] Benchmark current array parsing performance
  - [ ] Implement optimized parsing algorithm
  - [ ] Add support for PostgreSQL array types
  - [ ] Create performance tests
  - [ ] Consider using Npgsql's native array support

### 🗄️ **SQL Server Enhancements**
- [ ] **Make SQL command cache for specific tables or use parameterized queries**
  - [ ] Analyze query patterns for caching opportunities
  - [ ] Implement table-specific command cache
  - [ ] Switch to parameterized queries where missing
  - [ ] Add cache invalidation strategy

### 🧪 **Testing**
- [ ] **Unit Tests**
  - [ ] Increase code coverage to 90% >
  - [ ] Add edge case scenarios
- [ ] **Integration Tests**
  - [ ] Test with real database instances
  - [ ] Add multi-database compatibility tests
  - [ ] Test concurrent operations
- [ ] **Performance Tests**
  - [ ] Create benchmark suite
  - [ ] Test under load (1000+ concurrent requests)
  - [ ] Monitor memory leaks

### 🔄 **Future Considerations**
- [ ] Add support for additional database providers(Redis? + DbContext Interceptor?)
- [ ] Implement async/await pattern throughout
