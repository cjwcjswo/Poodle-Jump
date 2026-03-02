# Poodle Jump

원통형 맵을 오르며 점프하는 캐주얼 게임입니다.  
Unity 클라이언트와 ASP.NET Core + Redis 랭킹 API 서버로 구성됩니다.

## 구조

- **Poodle Jump/** – Unity 프로젝트 (클라이언트)
- **Server/** – 랭킹 API (ASP.NET Core 8, Redis)

## 요구 사항

- **클라이언트**: Unity 6 (또는 호환 에디터)
- **서버**: .NET 8 SDK, Redis

## 로컬 실행

### 랭킹 서버 (Docker)

```bash
# Redis + API 서버 기동 (API: http://localhost:5000)
docker-compose up -d

# API 키 지정 (선택)
export ApiSettings__ValidKey=your-api-key
docker-compose up -d
```

### 랭킹 서버 (직접 실행)

1. Redis 실행 (예: `redis-server`, 포트 6379)
2. `Server/`에서:
   ```bash
   dotnet run
   ```
3. API: `http://localhost:5000` (또는 launchSettings 기준)

### Unity 클라이언트

1. Unity Hub에서 `Poodle Jump` 폴더를 프로젝트로 추가
2. 씬 열기 후 Play
3. 랭킹/닉네임 사용 시 클라이언트의 API Base URL을 서버 주소로 설정 (예: `http://localhost:5000/api/Ranking`)

## 주요 기능

- 원통 위 점프 플레이 (키보드 + 기울기 하이브리드 입력)
- 리워드 광고 1회 부활
- 랭킹 제출 및 타이틀/인게임 랭킹 표시
- 닉네임 설정 및 타이틀 복귀

## 라이선스

Private / 개인 프로젝트
