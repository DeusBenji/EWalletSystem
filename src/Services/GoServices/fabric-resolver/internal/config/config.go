package config

import (
	"fmt"
	"os"
	"strconv"
	"time"
)

type Config struct {
	Server ServerConfig
	Fabric FabricConfig
}

type ServerConfig struct {
	Port         int
	ReadTimeout  time.Duration
	WriteTimeout time.Duration
	IdleTimeout  time.Duration
}

type FabricConfig struct {
	NetworkConfig string
	ChannelID     string
	ChaincodeName string
	OrgName       string
	UserName      string
	MspID         string
}

func Load() (*Config, error) {
	cfg := &Config{
		Server: ServerConfig{
			Port:         getEnvAsInt("SERVER_PORT", 8080),
			ReadTimeout:  getEnvAsDuration("SERVER_READ_TIMEOUT", 15*time.Second),
			WriteTimeout: getEnvAsDuration("SERVER_WRITE_TIMEOUT", 15*time.Second),
			IdleTimeout:  getEnvAsDuration("SERVER_IDLE_TIMEOUT", 60*time.Second),
		},
		Fabric: FabricConfig{
			NetworkConfig: getEnv("FABRIC_NETWORK_CONFIG", "./config/network.yaml"),
			ChannelID:     getEnv("FABRIC_CHANNEL_ID", "mychannel"),
			ChaincodeName: getEnv("FABRIC_CHAINCODE_NAME", "verifiable-credentials"),
			OrgName:       getEnv("FABRIC_ORG_NAME", "Org1"),
			UserName:      getEnv("FABRIC_USER_NAME", "Admin"),
			MspID:         getEnv("FABRIC_MSP_ID", "Org1MSP"),
		},
	}

	if err := cfg.validate(); err != nil {
		return nil, err
	}

	return cfg, nil
}

func (c *Config) validate() error {
	if c.Server.Port <= 0 || c.Server.Port > 65535 {
		return fmt.Errorf("invalid server port: %d", c.Server.Port)
	}

	if c.Fabric.ChannelID == "" {
		return fmt.Errorf("fabric channel ID is required")
	}

	if c.Fabric.ChaincodeName == "" {
		return fmt.Errorf("fabric chaincode name is required")
	}

	return nil
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

func getEnvAsInt(key string, defaultValue int) int {
	valueStr := os.Getenv(key)
	if valueStr == "" {
		return defaultValue
	}

	value, err := strconv.Atoi(valueStr)
	if err != nil {
		return defaultValue
	}

	return value
}

func getEnvAsDuration(key string, defaultValue time.Duration) time.Duration {
	valueStr := os.Getenv(key)
	if valueStr == "" {
		return defaultValue
	}

	value, err := time.ParseDuration(valueStr)
	if err != nil {
		return defaultValue
	}

	return value
}
