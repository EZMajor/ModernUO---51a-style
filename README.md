# 51a-Style ModernUO

[![GitHub license](https://img.shields.io/github/license/EZMajor/51a-style-ModernUo?color=blue)](https://github.com/EZMajor/51a-style-ModernUo/blob/master/LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/EZMajor/51a-style-ModernUo?logo=github&style=flat)](https://github.com/EZMajor/51a-style-ModernUo/stargazers)
[![GitHub issues](https://img.shields.io/github/issues/EZMajor/51a-style-ModernUo?logo=github)](https://github.com/EZMajor/51a-style-ModernUo/issues)
[![.NET](https://img.shields.io/badge/.NET-9.0-5C2D91?logo=.NET&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/9.0)

**Authentic Classic PvP in a Modern Server Framework**

51a-Style ModernUO brings the responsive, deterministic combat timing of Sphere 0.51a to the modern era. Experience server-authoritative PvP with ±25ms precision while leveraging ModernUO's scalable, production-ready architecture.

## What Makes 51a-Style Unique

This fork integrates the **Sphere51a Combat System** - a high-performance module that delivers:

- **Deterministic Timing**: Server-controlled ±25ms PvP precision
- **Independent Action Timers**: Separate swing, spell, bandage, and wand mechanics
- **Scalable Architecture**: O(active combatants) performance, handles 500+ concurrent players
- **Zero Core Modifications**: Clean integration with minimal hooks
- **Production Verified**: Load tested and optimized for real-world deployment

## Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/downloads)

### Installation

```bash
# Clone the repository
git clone https://github.com/EZMajor/51a-style-ModernUo.git
cd 51a-style-ModernUo

# Build the server
./publish.cmd

# Configure Sphere51a combat system
# Edit Distribution/Data/modernuo.json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true
  }
}

# Run the server
cd Distribution
./ModernUO.exe
```

### Verify Installation
In-game, use the command:
```
/VerifyWeaponTiming
```

Expected output confirms Sphere51a is active.

## Key Features

### Combat System
- **Global 50ms Tick**: Deterministic timing for all combat actions
- **Dexterity Scaling**: Authentic weapon speed calculations with dex bonuses
- **Action Independence**: Swing, cast, heal, and wand actions don't interrupt each other
- **Performance Optimized**: <1ms average tick time, minimal server impact

### ModernUO Foundation
- **Cross-Platform**: Windows, Linux, macOS support
- **Production Ready**: Enterprise-grade server architecture
- **Extensible**: Full scripting and customization capabilities
- **Community Driven**: Active development and support

### Tools & Monitoring
- **Real-Time Performance**: Built-in monitoring commands
- **Load Testing**: Synthetic combat simulation tools
- **Audit System**: Comprehensive combat logging and verification
- **Configuration Guides**: Detailed setup and tuning documentation

## Documentation

- **[Sphere51a Module README](Projects/UOContent/Modules/Sphere51a/README.md)** - Complete combat system documentation
- **[Configuration Guide](Projects/UOContent/Modules/Sphere51a/CONFIGURATION_GUIDE.md)** - Setup and tuning
- **[Building Guide](docs/building-server.md)** - Server compilation instructions
- **[Installation Guide](docs/installation.md)** - Deployment procedures

## Performance Benchmarks

| Metric | Target | Achieved |
|--------|--------|----------|
| Combat Tick Time | ≤5ms | <1ms typical |
| Memory per Combatant | ~200 bytes | Optimized |
| Scalability | O(active) | Linear scaling |
| CPU Impact | <10% | <5% typical |

*Verified through extensive load testing with 500+ simulated combatants*

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup
```bash
# Fork and clone
git clone https://github.com/YOUR_USERNAME/51a-style-ModernUo.git

# Open in your IDE
# Visual Studio 2022, Rider, or VS Code recommended

# Build and test
dotnet build ModernUO.sln
dotnet test
```

## License

This project is licensed under the same terms as ModernUO. See [LICENSE](LICENSE) for details.

## Acknowledgments

- **ModernUO Team**: For the excellent server foundation
- **Sphere Community**: For pioneering deterministic PvP mechanics
- **Open Source Community**: For the tools and libraries that make this possible

## Support

- **GitHub Issues**: [Report bugs and request features](https://github.com/EZMajor/51a-style-ModernUo/issues)
- **Discord**: Join the ModernUO community
- **Documentation**: Check the [Sphere51a guides](Projects/UOContent/Modules/Sphere51a/) for troubleshooting

---

**Experience authentic classic PvP timing in a modern, scalable server framework.**
