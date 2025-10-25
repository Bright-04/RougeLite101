# RougeLite101 Developer Documentation

Welcome to the RougeLite101 developer documentation! This folder contains comprehensive guides and references for the project.

## 📚 Documentation Index

### 🛡️ Code Quality & Robustness
- **[Null Checks Implementation Guide](NULL_CHECKS_GUIDE.md)** - Comprehensive guide to null checks implementation
- **[Null Checks Quick Reference](NULL_CHECKS_QUICK_REFERENCE.md)** - Quick patterns and templates

### 🔄 Event System Architecture
- **[Event System Guide](EVENT_SYSTEM_GUIDE.md)** - Complete event-driven architecture documentation
- **[Event System Quick Reference](EVENT_SYSTEM_QUICK_REFERENCE.md)** - Quick setup and usage patterns

### 🎮 Game Systems *(Coming Soon)*
- **Input System Guide** - Player controls and input handling

#### How to Set Up the Input System and Create an Input Action Asset

1. **Enable the Input System Package**
	- Go to `Edit > Project Settings > Player > Other Settings`.
	- Under **Active Input Handling**, select **Input System Package (New)** or **Both**.

2. **Create the Input Actions Asset**
	- In the **Project** window, right-click in your desired folder (e.g., `Assets/Input`).
	- Select **Create > Input Actions**.
	- Name the asset (e.g., `PlayerInputActions`).

3. **Open and Configure the Asset**
	- Double-click the new asset to open the Input Actions editor.
	- Click the **"+"** button to add an **Action Map** (e.g., "Player").
	- Inside the Action Map, click **"+"** to add actions:
	  - **Move**: Set type to **Value**, Control Type to **Vector2**.
	  - **FastMove**: Set type to **Button**.

4. **Add Bindings**
	- For **Move**: Add a binding, set Path to `<Keyboard>/wasd` or `<Keyboard>/arrow keys` (or use the composite "2D Vector" for WASD/Arrows).
	- For **FastMove**: Add a binding, set Path to `<Keyboard>/leftShift`.

5. **Save the Asset**

6. **Assign in Inspector**
	- In your script component (e.g., `SimplePlayerMovement`), assign the created actions to the `moveAction` and `fastMoveAction` fields.

**Tip:** You can also use the `PlayerInput` component for automatic event handling.
- **Combat System Guide** - Weapon and spell systems
- **AI System Guide** - Enemy behavior and pathfinding

### 🏗️ Architecture *(Coming Soon)*
- **Project Structure** - Folder organization and naming conventions
- **Component Dependencies** - Required components for each system
- **Performance Guidelines** - Optimization best practices

### 🔧 Development Workflow *(Coming Soon)*
- **Setup Guide** - Getting started with development
- **Build Configuration** - Platform-specific build settings
- **Testing Guidelines** - Unit testing and quality assurance

## 🚀 Quick Start

For immediate help with common issues:

1. **Game Crashing?** → Check [Null Checks Guide](NULL_CHECKS_GUIDE.md#troubleshooting)
2. **Missing Components?** → See [Quick Reference](NULL_CHECKS_QUICK_REFERENCE.md)
3. **Need Event System?** → Start with [Event System Quick Reference](EVENT_SYSTEM_QUICK_REFERENCE.md)
4. **New to Project?** → Read the comprehensive guides starting with Null Checks

## 📝 Contributing to Documentation

When adding new systems or making changes:

1. Update relevant guides in this `Docs/` folder
2. Add new guides for major systems
3. Keep the quick reference updated
4. Include code examples and troubleshooting tips

## 📧 Support

For questions about the documentation or implementation:
- Check the [Troubleshooting](NULL_CHECKS_GUIDE.md#troubleshooting) section
- Review code comments in the actual scripts
- Refer to Unity's official documentation for framework-specific questions

---

**Last Updated**: September 2025  
**Project**: RougeLite101  
**Unity Version**: 6000.1.9f1
 
### ??? Architecture
- **[Architecture Overview](ARCHITECTURE.md)** - Systems, patterns, and data flow
- **[Code Map](CODEMAP.md)** - File‑by‑file responsibilities and where things live
- **[Runtime Flow](RUNTIME_FLOW.md)** - What happens at boot and during play
- **[Extending the Project](EXTENDING.md)** - How to add content the “project way”
- **[Diagrams](DIAGRAMS.md)** - Mermaid diagrams of key systems and flows
- **[Structure](STRUCTURE.md)** - Detailed codebase structure and file roles
