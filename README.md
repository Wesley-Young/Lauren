# Lauren

Stabilizer quantum circuit experimental platform on .NET, with extended support for Majorana-based qubits, and embraces Stim ecosystem.

## Roadmap

### Stage 1: Migration of ExtendedStim

Re-implement everything from the [original repository](https://github.com/Moke2001/ExtendedStim), which contains:
- a circuit DSL
- some built-in code presets
- a DEM (detector error model) compiler

All of those are currently in Python.

### Stage 2: Decoding backend

Build a decoding backend for the compiled DEMs, allowing for end-to-end simulation of stabilizer circuits with noise and decoding. The potentially targeting algorithms are BPOSD, MWPM, or Tesseract.

### Stage 3: Improved decoding algorithms

Research and implement improved decoding algorithms, potentially leveraging machine learning techniques or more advanced classical algorithms to enhance decoding performance and accuracy.
