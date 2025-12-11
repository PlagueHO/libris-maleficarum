#!/bin/bash
set -e

echo "🚀 Starting devcontainer post-create setup..."

# Install Azure Developer CLI (azd)
echo "📦 Installing Azure Developer CLI (azd)..."
if ! command -v azd &> /dev/null; then
    curl -fsSL https://aka.ms/install-azd.sh | bash
    echo "✅ Azure Developer CLI (azd) installed successfully"
else
    echo "✅ Azure Developer CLI (azd) already installed"
fi

# Install Aspire CLI (global dotnet tool)
echo "📦 Installing .NET Aspire CLI..."
if ! dotnet tool list -g | grep -q "aspire"; then
    dotnet tool install -g Aspire.Cli --prerelease
    echo "✅ Aspire CLI installed successfully"
else
    echo "✅ Aspire CLI already installed"
fi

# Install frontend dependencies
echo "📦 Installing frontend dependencies (pnpm)..."
if [ -d "libris-maleficarum-app" ]; then
    cd libris-maleficarum-app
    pnpm install
    echo "✅ Frontend dependencies installed successfully"
    cd ..
else
    echo "⚠️  Frontend app directory not found, skipping pnpm install"
fi

echo "✨ Devcontainer setup complete!"
