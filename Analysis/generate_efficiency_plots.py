import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import os  # Import 'os' to create directories
from matplotlib.axes import Axes  # Import Axes type for type hinting

# --- Configuration ---
CSV_FILE_PATH = '../ExperimentResults/TCC_Experiment_Results.csv'
PLOT_OUTPUT_DIR = 'plots/'

# --- Map for renaming techniques to TCC-compliant names ---
TECHNIQUE_MAP = {
    "AgentBasedRandomWalk": "Abordagem Baseada em Agentes",
    "LSystem": "L-systems",
    "PathBasedAStarPOIs": "Algoritmos Baseados em Grafos"
}

# --- Fixed Technique Order ---
TECHNIQUE_ORDER = [
    "Abordagem Baseada em Agentes",
    "L-systems",
    "Algoritmos Baseados em Grafos"
]


# --- 1. Setup ---
def setup_analysis():
    """
    Sets the plot theme and ensures the plot output directory exists.
    """
    sns.set_theme(style="whitegrid", palette="muted")

    if not os.path.exists(PLOT_OUTPUT_DIR):
        os.makedirs(PLOT_OUTPUT_DIR)
        print(f"Created directory: {PLOT_OUTPUT_DIR}")


# --- 2. Load Data ---
def load_data(file_path):
    """
    Loads the CSV data from the specified path.
    """
    if not os.path.exists(file_path):
        print(f"Error: Data file not found at {file_path}")
        print("Please ensure the CSV file is in the 'ExperimentResults' folder.")
        return None

    print(f"Loading data from {file_path}...")
    df = pd.read_csv(file_path)
    print(f"Data loaded successfully. Shape: {df.shape}")

    # Use the map to replace the C# names with the formal TCC names
    df['Technique'] = df['Technique'].map(TECHNIQUE_MAP).fillna(df['Technique'])
    print(f"Renamed techniques to: {df['Technique'].unique()}")

    df['MemoryUsed_MB'] = df['MemoryUsed_bytes'] / (1024 * 1024)
    return df


# --- 3. Annotation Helper Function (Linter-Safe) ---
def add_bar_annotations(
        ax: Axes,
        df: pd.DataFrame,
        col_mean: tuple,
        col_std: tuple,
        unit: str,
        precision_low: int,
        precision_high: int
):
    """
    Helper function to add mean/std annotations above bar plots.
    Iterates over the dataframe and places text labels on the given Axes.
    """

    # --- CHANGE 1: Use enumerate(iterrows()) ---
    # We use 'enumerate' to get a guaranteed integer 'i' (0, 1, 2)
    # We discard the 'idx' from iterrows() (which the linter saw as 'Hashable')
    for i, (idx, row) in enumerate(df.iterrows()):

        # --- CHANGE 2: Use .get() for value retrieval ---
        # Using .get() is safer for linters than direct bracket access
        # with tuple keys. We then explicitly cast the raw object to float.
        mean_val_raw = row.get(col_mean)
        std_val_raw = row.get(col_std)

        # This explicit cast should now satisfy the linter,
        # as 'mean_val_raw' is seen as a generic object, not a 'Series'.
        mean_val = float(mean_val_raw)
        # -------------------------------------------------

        # Check for NaN and cast the final std_val to float
        if pd.isna(std_val_raw):
            std_val = 0.0
        else:
            std_val = float(std_val_raw)

        # Calculate Y position
        y_position = (mean_val + std_val) * 1.2

        # Format the text label
        if mean_val < 1.0:
            text_label = f"({mean_val:.{precision_low}f} {unit})"
        else:
            text_label = f"({mean_val:.{precision_high}f} {unit})"

        # Add the text to the plot
        ax.text(
            # --- CHANGE 3: Use the guaranteed int 'i' ---
            # 'i' now comes from 'enumerate', so it's a definite int,
            # which safely casts to a float for the coordinate.
            float(i),
            y_position,
            # -------------------------------------------
            text_label,
            color='black',
            ha='center',
            va='bottom',
            fontsize=10
        )


# --- 4. Generate Efficiency Plots (Objective 7.4.1) ---
def create_efficiency_plots(df):
    """
    Generates and saves the Computational Efficiency comparison plots.
    """
    if df is None:
        print("Cannot create plots. Data not loaded.")
        return

    print("Generating Computational Efficiency plots (Bar Charts with Annotations)...")

    # --- Aggregate data ---
    df_summary = df.groupby('Technique')[['GenerationTime_s', 'MemoryUsed_MB']].agg(
        ['mean', 'std']
    ).reset_index()

    # --- Reorder the Summary Dataframe ---
    try:
        df_summary_ordered = df_summary.set_index('Technique').reindex(TECHNIQUE_ORDER).reset_index()
    except Exception as e:
        print(f"Error reordering dataframe. Check if all techniques in TECHNIQUE_ORDER"
              f" exist in the CSV and were mapped correctly. Error: {e}")
        df_summary_ordered = df_summary

    # --- Plot 1: Generation Time (Bar Chart) ---
    plt.figure(figsize=(10, 7))

    ax_time = sns.barplot(
        x='Technique',
        y=('GenerationTime_s', 'mean'),
        data=df_summary_ordered,
        capsize=0.1
    )

    plt.errorbar(
        x=df_summary_ordered['Technique'],
        y=df_summary_ordered[('GenerationTime_s', 'mean')],
        yerr=df_summary_ordered[('GenerationTime_s', 'std')],
        fmt='none',
        c='black',
        capsize=5
    )

    ax_time.set_yscale('log')

    # Plot titles and labels (Portuguese)
    ax_time.set_title('Comparação do Tempo Médio de Geração por Técnica (30 Repetições)', fontsize=16)
    ax_time.set_xlabel('Técnica de Geração', fontsize=12)
    ax_time.set_ylabel('Tempo Médio de Geração (segundos, escala log)', fontsize=12)

    # --- Call Annotation Helper for Time ---
    add_bar_annotations(
        ax=ax_time,
        df=df_summary_ordered,
        col_mean=('GenerationTime_s', 'mean'),
        col_std=('GenerationTime_s', 'std'),
        unit="s",
        precision_low=4,
        precision_high=1
    )

    plt.tight_layout()
    time_plot_path = os.path.join(PLOT_OUTPUT_DIR, '01_efficiency_time_comparison_barchart_annotated.png')
    plt.savefig(time_plot_path)
    plt.close()
    print(f"Saved: {time_plot_path}")

    # --- Plot 2: Memory Usage (Bar Chart) ---
    plt.figure(figsize=(10, 7))

    ax_mem = sns.barplot(
        x='Technique',
        y=('MemoryUsed_MB', 'mean'),
        data=df_summary_ordered,
        capsize=0.1
    )

    plt.errorbar(
        x=df_summary_ordered['Technique'],
        y=df_summary_ordered[('MemoryUsed_MB', 'mean')],
        yerr=df_summary_ordered[('MemoryUsed_MB', 'std')],
        fmt='none',
        c='black',
        capsize=5
    )

    ax_mem.set_yscale('log')

    # Plot titles and labels (Portuguese)
    ax_mem.set_title('Comparação do Uso Médio de Memória por Técnica (30 Repetições)', fontsize=16)
    ax_mem.set_xlabel('Técnica de Geração', fontsize=12)
    ax_mem.set_ylabel('Uso Médio de Memória (Megabytes, escala log)', fontsize=12)

    # --- Call Annotation Helper for Memory ---
    add_bar_annotations(
        ax=ax_mem,
        df=df_summary_ordered,
        col_mean=('MemoryUsed_MB', 'mean'),
        col_std=('MemoryUsed_MB', 'std'),
        unit="MB",
        precision_low=2,
        precision_high=1
    )

    plt.tight_layout()
    memory_plot_path = os.path.join(PLOT_OUTPUT_DIR, '02_efficiency_memory_comparison_barchart_annotated.png')
    plt.savefig(memory_plot_path)
    plt.close()
    print(f"Saved: {memory_plot_path}")


# --- Main execution ---
if __name__ == "__main__":
    setup_analysis()
    dataframe = load_data(CSV_FILE_PATH)
    if dataframe is None:
        print("Analysis script aborted due to missing data.")
    else:
        create_efficiency_plots(dataframe)
        print("Analysis script finished.")