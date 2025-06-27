#!/bin/bash

echo "=== Test Redis và UserCache Performance ==="
echo

# Test 1: Register user mới
echo "1. Registering new user..."
REGISTER_RESPONSE=$(curl -s -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "perftest", "email": "perftest@example.com", "password": "Password123"}')

echo "Register response: $REGISTER_RESPONSE"
echo

# Test 2: Login lần đầu (cache miss)
echo "2. First login (cache miss)..."
START_TIME=$(date +%s%N)
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "perftest@example.com", "password": "Password123"}')
END_TIME=$(date +%s%N)
FIRST_LOGIN_TIME=$((END_TIME - START_TIME))

echo "First login response: $LOGIN_RESPONSE"
echo "First login time: ${FIRST_LOGIN_TIME} nanoseconds"
echo

# Test 3: Login lần thứ 2 (cache hit)
echo "3. Second login (cache hit)..."
START_TIME=$(date +%s%N)
LOGIN_RESPONSE2=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "perftest@example.com", "password": "Password123"}')
END_TIME=$(date +%s%N)
SECOND_LOGIN_TIME=$((END_TIME - START_TIME))

echo "Second login response: $LOGIN_RESPONSE2"
echo "Second login time: ${SECOND_LOGIN_TIME} nanoseconds"
echo

# Test 4: Login lần thứ 3 (cache hit)
echo "4. Third login (cache hit)..."
START_TIME=$(date +%s%N)
LOGIN_RESPONSE3=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "perftest@example.com", "password": "Password123"}')
END_TIME=$(date +%s%N)
THIRD_LOGIN_TIME=$((END_TIME - START_TIME))

echo "Third login response: $LOGIN_RESPONSE3"
echo "Third login time: ${THIRD_LOGIN_TIME} nanoseconds"
echo

# Tính toán performance
echo "=== Performance Analysis ==="
echo "First login (cache miss): ${FIRST_LOGIN_TIME} ns"
echo "Second login (cache hit): ${SECOND_LOGIN_TIME} ns"
echo "Third login (cache hit): ${THIRD_LOGIN_TIME} ns"

if [ $SECOND_LOGIN_TIME -lt $FIRST_LOGIN_TIME ]; then
    IMPROVEMENT=$((FIRST_LOGIN_TIME - SECOND_LOGIN_TIME))
    PERCENTAGE=$((IMPROVEMENT * 100 / FIRST_LOGIN_TIME))
    echo "Performance improvement: ${IMPROVEMENT} ns (${PERCENTAGE}%)"
else
    echo "No performance improvement detected"
fi

echo

# Test 5: Kiểm tra Redis data
echo "5. Checking Redis data..."
echo "User cache keys:"
docker exec -it redis redis-cli KEYS "user:*perftest*" 2>/dev/null || echo "No user cache found"

echo "User email cache keys:"
docker exec -it redis redis-cli KEYS "user_email:*perftest*" 2>/dev/null || echo "No email cache found"

echo "Session keys:"
docker exec -it redis redis-cli KEYS "session:*" | wc -l | xargs echo "Total sessions:"

echo
echo "=== Test completed ===" 