#!/bin/bash

echo "=== Demo Chi Tiết Redis và UserCache ==="
echo

# Tạo user mới
echo "1. Tạo user mới..."
USER_ID=$(curl -s -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "demouser", "email": "demo@example.com", "password": "Password123"}' | \
  jq -r '.token' | cut -d'.' -f2 | base64 -d | jq -r '.sub')

echo "User ID: $USER_ID"
echo

# Kiểm tra Redis trước khi login
echo "2. Redis trước khi login:"
echo "User cache:"
docker exec -it redis redis-cli GET "user:$USER_ID" 2>/dev/null || echo "Chưa có cache"
echo "Email cache:"
docker exec -it redis redis-cli GET "user_email:demo@example.com" 2>/dev/null || echo "Chưa có cache"
echo

# Login lần đầu
echo "3. Login lần đầu (cache miss)..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "demo@example.com", "password": "Password123"}')

echo "Login response: $LOGIN_RESPONSE"
echo

# Kiểm tra Redis sau khi login
echo "4. Redis sau khi login:"
echo "User cache:"
docker exec -it redis redis-cli GET "user:$USER_ID" 2>/dev/null | jq '.' || echo "Không có data"
echo "Email cache:"
docker exec -it redis redis-cli GET "user_email:demo@example.com" 2>/dev/null || echo "Không có data"
echo

# Kiểm tra sessions
echo "5. Sessions trong Redis:"
echo "User sessions hash:"
docker exec -it redis redis-cli HGETALL "user_sessions:$USER_ID" 2>/dev/null || echo "Không có sessions"
echo "Session keys:"
docker exec -it redis redis-cli KEYS "session:$USER_ID:*" 2>/dev/null || echo "Không có session keys"
echo

# Login lần thứ 2
echo "6. Login lần thứ 2 (cache hit)..."
LOGIN_RESPONSE2=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "demo@example.com", "password": "Password123"}')

echo "Login response: $LOGIN_RESPONSE2"
echo

# Kiểm tra sessions sau login lần 2
echo "7. Sessions sau login lần 2:"
docker exec -it redis redis-cli HGETALL "user_sessions:$USER_ID" 2>/dev/null || echo "Không có sessions"
echo

# Test validate token
echo "8. Test validate token..."
TOKEN=$(echo $LOGIN_RESPONSE2 | jq -r '.token')
VALIDATE_RESPONSE=$(curl -s -X POST http://localhost:5001/api/auth/validate \
  -H "Content-Type: application/json" \
  -d "{\"token\": \"$TOKEN\"}")

echo "Validate response: $VALIDATE_RESPONSE"
echo

# Test logout
echo "9. Test logout..."
LOGOUT_RESPONSE=$(curl -s -X POST http://localhost:5001/api/auth/logout \
  -H "Authorization: Bearer $TOKEN")

echo "Logout response: $LOGOUT_RESPONSE"
echo

# Kiểm tra blacklist
echo "10. Kiểm tra blacklist:"
BLACKLIST_COUNT=$(docker exec -it redis redis-cli KEYS "blacklist:*" | wc -l)
echo "Số lượng blacklisted tokens: $BLACKLIST_COUNT"
echo "Blacklist keys:"
docker exec -it redis redis-cli KEYS "blacklist:*" | tail -3 2>/dev/null || echo "Không có blacklist"
echo

# Test validate token sau logout
echo "11. Test validate token sau logout..."
VALIDATE_RESPONSE2=$(curl -s -X POST http://localhost:5001/api/auth/validate \
  -H "Content-Type: application/json" \
  -d "{\"token\": \"$TOKEN\"}")

echo "Validate response sau logout: $VALIDATE_RESPONSE2"
echo

# Kiểm tra TTL của cache
echo "12. Kiểm tra TTL của cache:"
echo "User cache TTL:"
docker exec -it redis redis-cli TTL "user:$USER_ID" 2>/dev/null || echo "Không có TTL"
echo "Email cache TTL:"
docker exec -it redis redis-cli TTL "user_email:demo@example.com" 2>/dev/null || echo "Không có TTL"
echo "Session TTL:"
docker exec -it redis redis-cli TTL "user_sessions:$USER_ID" 2>/dev/null || echo "Không có TTL"
echo

echo "=== Demo hoàn thành ===" 