from PySpice.Spice.Netlist import Circuit
from PySpice.Unit import *
import sys
import json

# -------------------------------
# --- Load the breadboard JSON ---
# -------------------------------
if len(sys.argv) < 2:
    raise ValueError("No file path argument!")

file_path = sys.argv[1]

with open(file_path, "r") as f:
    board = json.load(f)

# -------------------------------
# --- Define breadboard layout ---
# -------------------------------
node_grid = [
    ["VCC"] * 18,
    ["AC1", "AC2", "AC3", "AC4", "AC5", "AC6", "AC7", "AC8", "AC9", "AC10", "AC11", "AC12", "AC13", "AC14", "AC15", "AC16", "AC17", "AC18"],
    ["AC1", "AC2", "AC3", "AC4", "AC5", "AC6", "AC7", "AC8", "AC9", "AC10", "AC11", "AC12", "AC13", "AC14", "AC15", "AC16", "AC17", "AC18"],
    ["AC1", "AC2", "AC3", "AC4", "AC5", "AC6", "AC7", "AC8", "AC9", "AC10", "AC11", "AC12", "AC13", "AC14", "AC15", "AC16", "AC17", "AC18"],
    ["AC1", "AC2", "AC3", "AC4", "AC5", "AC6", "AC7", "AC8", "AC9", "AC10", "AC11", "AC12", "AC13", "AC14", "AC15", "AC16", "AC17", "AC18"],
    ["BC1", "BC2", "BC3", "BC4", "BC5", "BC6", "BC7", "BC8", "BC9", "BC10", "BC11", "BC12", "BC13", "BC14", "BC15", "BC16", "BC17", "BC18"],
    ["BC1", "BC2", "BC3", "BC4", "BC5", "BC6", "BC7", "BC8", "BC9", "BC10", "BC11", "BC12", "BC13", "BC14", "BC15", "BC16", "BC17", "BC18"],
    ["BC1", "BC2", "BC3", "BC4", "BC5", "BC6", "BC7", "BC8", "BC9", "BC10", "BC11", "BC12", "BC13", "BC14", "BC15", "BC16", "BC17", "BC18"],
    ["BC1", "BC2", "BC3", "BC4", "BC5", "BC6", "BC7", "BC8", "BC9", "BC10", "BC11", "BC12", "BC13", "BC14", "BC15", "BC16", "BC17", "BC18"],
    ["GND"] * 18
]

# -------------------------------
# --- Map components to nodes ---
# -------------------------------
component_nodes = {}

board[0][0] = "VCC"
board[9][0] = "GND"

for r in range(10):
    for c in range(18):
        comp = board[r][c]
        if comp == "":
            continue
        node = node_grid[r][c]

        # Wires and resistors
        if comp.startswith(("W", "R")):
            component_nodes.setdefault(comp, [])
            if node not in component_nodes[comp]:
                component_nodes[comp].append(node)

        # LEDs (LAx, LCx)
        elif comp.startswith("LA"):
            led_id = comp[2:]
            component_nodes.setdefault(f"L{led_id}", {})["A"] = node
        elif comp.startswith("LC"):
            led_id = comp[2:]
            component_nodes.setdefault(f"L{led_id}", {})["C"] = node

        # Buzzers (BAx, BCx)
        elif comp.startswith("BA"):
            buz_id = comp[2:]
            component_nodes.setdefault(f"B{buz_id}", {})["A"] = node
        elif comp.startswith("BC"):
            buz_id = comp[2:]
            component_nodes.setdefault(f"B{buz_id}", {})["C"] = node


# -------------------------------
# --- Convert to PySpice circuit ---
# -------------------------------
def dict_to_pypice_circuit(component_nodes, circuit_name="BreadboardCircuit"):
    circuit = Circuit(circuit_name)

    # LED model
    circuit.model('LED', 'D', IS=1e-20, N=2, RS=10@u_Ohm, BV=100, IBV=0.1@u_A)

    # Buzzer model (as a resistor of ~50 Ω)
    BUZZER_RESISTANCE = 50@u_Ohm

    for comp, nodes in component_nodes.items():
        if isinstance(nodes, list) and len(nodes) == 2:
            # Regular 2-pin components (wire/resistor)
            n1, n2 = nodes
            if n1 == "GND":
                n1 = circuit.gnd
            if n2 == "GND":
                n2 = circuit.gnd
            if comp.startswith("W"):
                circuit.R(comp, n1, n2, 0.1@u_Ω)
            else:
                circuit.R(comp, n1, n2, 220@u_Ω)

        elif isinstance(nodes, dict) and "A" in nodes and "C" in nodes:
            # LEDs or Buzzers
            nA = nodes["A"]
            nC = nodes["C"]
            if nA == "GND":
                nA = circuit.gnd
            if nC == "GND":
                nC = circuit.gnd

            if comp.startswith("L"):
                circuit.D(comp, nA, nC, model="LED")
            elif comp.startswith("B"):
                circuit.R(comp, nA, nC, BUZZER_RESISTANCE)

    # Power supply
    circuit.V("input", "VCC", circuit.gnd, 5@u_V)
    return circuit


# -------------------------------
# --- Simulate circuit ---
# -------------------------------
circuit = dict_to_pypice_circuit(component_nodes)
simulator = circuit.simulator(temperature=25, nominal_temperature=25)
analysis = simulator.operating_point()

# -------------------------------
# --- Determine component status ---
# -------------------------------
def node_voltage(node_name, analysis):
    if str(node_name).upper() in ["GND", "0"]:
        return 0.0
    return float(analysis[str(node_name)][0])

LED_THRESHOLD = 1.5      # Volts
BUZZER_THRESHOLD = 0.5   # Volts

component_status = {}

for comp_name, nodes in component_nodes.items():
    if isinstance(nodes, dict):  # LEDs or buzzers
        n1 = nodes.get("A")
        n2 = nodes.get("C")
        v1 = node_voltage(n1, analysis)
        v2 = node_voltage(n2, analysis)
        v_diff = abs(v1 - v2)

        if comp_name.startswith("L"):
            status = "ON" if v_diff > LED_THRESHOLD else "OFF"
        elif comp_name.startswith("B"):
            status = "ON" if v_diff > BUZZER_THRESHOLD else "OFF"
        else:
            continue

        component_status[comp_name] = status

print(component_status)
