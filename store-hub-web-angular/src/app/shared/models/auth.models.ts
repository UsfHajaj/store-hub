export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponseDto {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
}
