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
    # Use 'enumerate' to get a guaranteed integer 'i' (0, 1, 2)
    for i, (idx, row) in enumerate(df.iterrows()):

        # Use .get() for value retrieval and cast explicitly to float
        mean_val_raw = row.get(col_mean)
        std_val_raw = row.get(col_std)

        mean_val = float(mean_val_raw)

        # Check for NaN and cast the final std_val to float
        if pd.isna(std_val_raw):
            std_val = 0.0
        else:
            std_val = float(std_val_raw)

        # Calculate Y position
        y_position = (mean_val + std_val) * 1.02

        if std_val == 0:
            y_position = mean_val * 1.02

        # Format the text label
        if mean_val < 1.0:
            text_label = f"({mean_val:.{precision_low}f} {unit})"
        else:
            text_label = f"({mean_val:.{precision_high}f} {unit})"

        # Add the text to the plot
        ax.text(
            float(i),  # Use the guaranteed int 'i' from enumerate
            y_position,
            text_label,
            color='black',
            ha='center',
            va='bottom',
            fontsize=10
        )


# --- 4. Generate Efficiency Plots (Objective 7.4.1) ---
def create_efficiency_plots(df, df_summary_ordered):
    """
    Generates and saves the Computational Efficiency comparison plots.
    """
    if df is None:
        print("Cannot create plots. Data not loaded.")
        return

    print("Generating Computational Efficiency plots (Bar Charts with Annotations)...")

    # --- Plot 1: Generation Time (Bar Chart) ---
    plt.figure(figsize=(10, 7))
    ax_time = sns.barplot(x='Technique', y=('GenerationTime_s', 'mean'), data=df_summary_ordered, capsize=0.1)
    plt.errorbar(
        x=df_summary_ordered['Technique'],
        y=df_summary_ordered[('GenerationTime_s', 'mean')],
        yerr=df_summary_ordered[('GenerationTime_s', 'std')],
        fmt='none', c='black', capsize=5
    )
    ax_time.set_yscale('log')
    ax_time.set_title('Comparação do Tempo Médio de Geração por Técnica (30 Repetições)', fontsize=16)
    ax_time.set_xlabel('Técnica de Geração', fontsize=12)
    ax_time.set_ylabel('Tempo Médio de Geração (segundos, escala log)', fontsize=12)

    add_bar_annotations(
        ax=ax_time, df=df_summary_ordered,
        col_mean=('GenerationTime_s', 'mean'), col_std=('GenerationTime_s', 'std'),
        unit="s", precision_low=4, precision_high=1
    )
    plt.tight_layout()
    time_plot_path = os.path.join(PLOT_OUTPUT_DIR, '01_efficiency_time_comparison_barchart_annotated.png')
    plt.savefig(time_plot_path)
    plt.close()
    print(f"Saved: {time_plot_path}")

    # --- Plot 2: Memory Usage (Bar Chart) ---
    plt.figure(figsize=(10, 7))
    ax_mem = sns.barplot(x='Technique', y=('MemoryUsed_MB', 'mean'), data=df_summary_ordered, capsize=0.1)
    plt.errorbar(
        x=df_summary_ordered['Technique'],
        y=df_summary_ordered[('MemoryUsed_MB', 'mean')],
        yerr=df_summary_ordered[('MemoryUsed_MB', 'std')],
        fmt='none', c='black', capsize=5
    )
    ax_mem.set_yscale('log')
    ax_mem.set_title('Comparação do Uso Médio de Memória por Técnica (30 Repetições)', fontsize=16)
    ax_mem.set_xlabel('Técnica de Geração', fontsize=12)
    ax_mem.set_ylabel('Uso Médio de Memória (Megabytes, escala log)', fontsize=12)

    add_bar_annotations(
        ax=ax_mem, df=df_summary_ordered,
        col_mean=('MemoryUsed_MB', 'mean'), col_std=('MemoryUsed_MB', 'std'),
        unit="MB", precision_low=2, precision_high=1
    )
    plt.tight_layout()
    memory_plot_path = os.path.join(PLOT_OUTPUT_DIR, '02_efficiency_memory_comparison_barchart_annotated.png')
    plt.savefig(memory_plot_path)
    plt.close()
    print(f"Saved: {memory_plot_path}")


# --- 5. Generate Structural Realism Plots (Objective 7.4.2) ---
def create_structural_realism_plots(df, df_summary_ordered):
    """
    Generates and saves the Structural Realism comparison plots (Bar Charts).
    """
    if df is None:
        print("Cannot create structural plots. Data not loaded.")
        return

    print("Generating Structural Realism plots (Bar Charts)...")

    # Define the metrics to plot
    # (Column Name, Plot Title, Y-Axis Label, Unit, Precision)
    metrics_to_plot_details = {
        'IntersectionCount_V': (
            'Contagem Média de Intersecções por Técnica (30 Repetições)',
            'Número Médio de Intersecções',
            '', 1  # Unit, Precision
        ),
        'AverageRoadLength': (
            'Comprimento Médio de Segmento por Técnica (30 Repetições)',
            'Comprimento Médio (metros)',
            'm', 1  # Unit, Precision
        ),
        'AverageCircuity': (
            'Circuidade Média da Malha por Técnica (30 Repetições)',
            'Circuidade Média (Índice)',
            '', 3  # Unit, Precision
        )
    }

    # Loop through each metric and create a plot
    for i, (metric, (title, ylabel, unit, precision)) in enumerate(metrics_to_plot_details.items()):

        # Check if the metric (mean) column exists in the summary dataframe
        if (metric, 'mean') not in df_summary_ordered.columns:
            print(f"Warning: Metric '{metric}' not found in aggregated data. Skipping plot.")
            continue

        plt.figure(figsize=(10, 7))

        # Use barplot on the pre-aggregated, ordered data
        ax = sns.barplot(
            x='Technique',
            y=(metric, 'mean'),
            data=df_summary_ordered,
            capsize=0.1
        )

        # Add the error bars
        plt.errorbar(
            x=df_summary_ordered['Technique'],
            y=df_summary_ordered[(metric, 'mean')],
            yerr=df_summary_ordered[(metric, 'std')],
            fmt='none', c='black', capsize=5
        )

        ax.set_title(title, fontsize=16)
        ax.set_xlabel('Técnica de Geração', fontsize=12)
        ax.set_ylabel(ylabel, fontsize=12)

        # We DO NOT use log scale here

        # Call the annotation helper
        add_bar_annotations(
            ax=ax, df=df_summary_ordered,
            col_mean=(metric, 'mean'), col_std=(metric, 'std'),
            unit=unit, precision_low=precision, precision_high=precision
        )

        plt.tight_layout()

        # Use a consistent naming convention (03, 04, 05...)
        plot_filename = f"{i + 3:02d}_structural_{metric.lower()}_barchart.png"
        plot_path = os.path.join(PLOT_OUTPUT_DIR, plot_filename)

        plt.savefig(plot_path)
        plt.close()
        print(f"Saved: {plot_path}")


# --- 6. NEW: Generate Functional Adaptability Plots (Objective 7.4.3) ---
def create_functional_adaptability_plots(df, df_summary_ordered):
    """
    Generates and saves the Functional Adaptability comparison plots (Bar Charts).
    """
    if df is None:
        print("Cannot create adaptability plots. Data not loaded.")
        return

    print("Generating Functional Adaptability plots (Bar Charts)...")

    # Define the metrics to plot
    # (Column Name, Plot Title, Y-Axis Label, Unit, Precision)
    metrics_to_plot_details = {
        'AverageRoadSteepness': (
            'Adaptação Média ao Terreno por Técnica (30 Repetições)',
            'Inclinação Média da Via (Índice)',
            '', 3  # Unit, Precision
        )
    }

    # Loop through each metric and create a plot
    # (i will start at 0, 0+6=6, so filename will be '06_...')
    for i, (metric, (title, ylabel, unit, precision)) in enumerate(metrics_to_plot_details.items()):

        # Check if the metric (mean) column exists in the summary dataframe
        if (metric, 'mean') not in df_summary_ordered.columns:
            print(f"Warning: Metric '{metric}' not found in aggregated data. Skipping plot.")
            continue

        plt.figure(figsize=(10, 7))

        # Use barplot on the pre-aggregated, ordered data
        ax = sns.barplot(
            x='Technique',
            y=(metric, 'mean'),
            data=df_summary_ordered,
            capsize=0.1
        )

        # Add the error bars
        plt.errorbar(
            x=df_summary_ordered['Technique'],
            y=df_summary_ordered[(metric, 'mean')],
            yerr=df_summary_ordered[(metric, 'std')],
            fmt='none', c='black', capsize=5
        )

        ax.set_title(title, fontsize=16)
        ax.set_xlabel('Técnica de Geração', fontsize=12)
        ax.set_ylabel(ylabel, fontsize=12)

        # We DO NOT use log scale here

        # Call the annotation helper
        add_bar_annotations(
            ax=ax, df=df_summary_ordered,
            col_mean=(metric, 'mean'), col_std=(metric, 'std'),
            unit=unit, precision_low=precision, precision_high=precision
        )

        plt.tight_layout()

        # Use a consistent naming convention (06...)
        plot_filename = f"{i + 6:02d}_adaptability_{metric.lower()}_barchart.png"
        plot_path = os.path.join(PLOT_OUTPUT_DIR, plot_filename)

        plt.savefig(plot_path)
        plt.close()
        print(f"Saved: {plot_path}")


# --- Main execution ---
if __name__ == "__main__":
    setup_analysis()
    dataframe = load_data(CSV_FILE_PATH)

    if dataframe is None:
        print("Analysis script aborted due to missing data.")
    else:

        # --- NEW: Aggregate ALL data ONCE at the start ---
        metrics_to_aggregate = [
            'GenerationTime_s', 'MemoryUsed_MB',
            'IntersectionCount_V', 'AverageRoadLength', 'AverageCircuity',
            'AverageRoadSteepness'  # <-- ADDED THIS METRIC
        ]

        # Filter list to only metrics that actually exist in the CSV
        existing_metrics = [m for m in metrics_to_aggregate if m in dataframe.columns]

        if len(existing_metrics) < len(metrics_to_aggregate):
            print("Warning: Not all expected metrics were found in the CSV.")

        # Aggregate the data
        df_summary = dataframe.groupby('Technique')[existing_metrics].agg(
            ['mean', 'std']
        ).reset_index()

        # Reorder the Summary Dataframe
        try:
            df_summary_ordered = df_summary.set_index('Technique').reindex(TECHNIQUE_ORDER).reset_index()
        except Exception as e:
            print(f"Error reordering dataframe. Check if all techniques in TECHNIQUE_ORDER"
                  f" exist in the CSV and were mapped correctly. Error: {e}")
            df_summary_ordered = df_summary
        # ---------------------------------------------------

        # Call the efficiency plots function
        create_efficiency_plots(dataframe, df_summary_ordered)

        # Call the structural realism plots function
        create_structural_realism_plots(dataframe, df_summary_ordered)

        # --- NEW: Call the adaptability plots function ---
        create_functional_adaptability_plots(dataframe, df_summary_ordered)

        print("Analysis script finished.")